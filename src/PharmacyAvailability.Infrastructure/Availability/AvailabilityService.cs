using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using PharmacyAvailability.Application.Availability;
using PharmacyAvailability.Contracts.Availability;
using PharmacyAvailability.Domain.Common;
using PharmacyAvailability.Domain.Common.Exceptions;
using PharmacyAvailability.Domain.Inventory;
using PharmacyAvailability.Domain.Pharmacies;
using PharmacyAvailability.Infrastructure.Persistence;

namespace PharmacyAvailability.Infrastructure.Availability;

public class AvailabilityService : IAvailabilityService
{
    private readonly PharmacyDbContext _context;
    private readonly ILogger<AvailabilityService> _logger;

    public AvailabilityService(PharmacyDbContext context, ILogger<AvailabilityService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<AvailabilitySearchResult> SearchAvailabilityAsync(
        AvailabilitySearchRequest request,
        CancellationToken ct = default)
    {
        // 1. Validate coordinates & radius
        var hasLat = request.Latitude.HasValue;
        var hasLon = request.Longitude.HasValue;

        if (hasLat != hasLon)
        {
            throw new DomainValidationException("Both latitude and longitude must be provided when searching by location.");
        }

        bool hasCoordinates = hasLat && hasLon;
        double userLat = 0, userLon = 0, radiusKm = 10.0;

        if (hasCoordinates)
        {
            userLat = request.Latitude!.Value;
            userLon = request.Longitude!.Value;

            if (userLat < -90.0 || userLat > 90.0)
                throw new DomainValidationException("Latitude must be between -90 and 90 degrees.");

            if (userLon < -180.0 || userLon > 180.0)
                throw new DomainValidationException("Longitude must be between -180 and 180 degrees.");

            if (request.RadiusKm.HasValue)
            {
                if (request.RadiusKm.Value <= 0.0)
                    throw new DomainValidationException("Radius must be greater than zero.");

                if (request.RadiusKm.Value > 100.0)
                    throw new DomainValidationException("Radius cannot exceed 100 kilometers.");

                radiusKm = request.RadiusKm.Value;
            }
        }

        // 2. Validate pagination
        var page = Math.Max(1, request.Page);
        var pageSize = Math.Clamp(request.PageSize, 1, 100);

        // 3. Resolve target medicine IDs if search filter criteria are provided
        var hasSpecificMedicine = request.MedicineId.HasValue;
        var hasBarcode = !string.IsNullOrWhiteSpace(request.Barcode);
        var hasAlias = !string.IsNullOrWhiteSpace(request.Alias);
        var hasQuery = !string.IsNullOrWhiteSpace(request.EffectiveQuery);
        var hasMedicineFilter = hasSpecificMedicine || hasBarcode || hasAlias || hasQuery;

        var targetMedicineIds = new HashSet<Guid>();

        if (hasSpecificMedicine)
        {
            targetMedicineIds.Add(request.MedicineId!.Value);
        }

        if (hasBarcode)
        {
            var normBarcode = request.Barcode!.Trim().ToUpperInvariant();
            var barcodeMedIds = await _context.MedicineBarcodes
                .AsNoTracking()
                .Where(b => b.Barcode == normBarcode)
                .Select(b => b.MedicineId)
                .ToListAsync(ct);

            foreach (var id in barcodeMedIds)
                targetMedicineIds.Add(id);
        }

        if (hasAlias)
        {
            var normAlias = request.Alias!.Trim().ToLowerInvariant();
            var aliasMedIds = await _context.MedicineAliases
                .AsNoTracking()
                .Where(a => a.Alias.ToLower() == normAlias)
                .Select(a => a.MedicineId)
                .ToListAsync(ct);

            foreach (var id in aliasMedIds)
                targetMedicineIds.Add(id);
        }

        if (hasQuery)
        {
            var normQuery = request.EffectiveQuery!.Trim().ToLowerInvariant();

            // Query matching brand name, generic name, active ingredient, barcode, or alias
            var queryMedIds = await _context.Medicines
                .AsNoTracking()
                .Where(m => m.IsActive &&
                    (m.BrandName.ToLower().Contains(normQuery) ||
                     m.GenericName.ToLower().Contains(normQuery) ||
                     m.ActiveIngredient.ToLower().Contains(normQuery) ||
                     m.Barcodes.Any(b => b.Barcode.ToLower().Contains(normQuery)) ||
                     m.Aliases.Any(a => a.Alias.ToLower().Contains(normQuery))))
                .Select(m => m.Id)
                .ToListAsync(ct);

            foreach (var id in queryMedIds)
                targetMedicineIds.Add(id);
        }

        // If a medicine filter was provided but found no matching medicines, return early
        if (hasMedicineFilter && targetMedicineIds.Count == 0)
        {
            return new AvailabilitySearchResult(
                Array.Empty<MedicineAvailabilityDto>(),
                page,
                pageSize,
                0,
                0,
                false,
                false);
        }

        // 4. Build database query with strict public filters
        var query = _context.PharmacyInventories
            .AsNoTracking()
            .Include(pi => pi.Medicine)
            .Include(pi => pi.Branch)
            .Include(pi => pi.Pharmacy)
            .Where(pi => pi.AvailableQuantity > 0)
            .Where(pi => pi.Pharmacy!.Status == PharmacyStatus.Active)
            .Where(pi => pi.Branch!.IsActive == true)
            .Where(pi => pi.Medicine!.IsActive == true);

        if (hasMedicineFilter)
        {
            query = query.Where(pi => targetMedicineIds.Contains(pi.MedicineId));
        }

        // 5. Apply bounding box reduction at the SQL level if coordinates provided
        if (hasCoordinates)
        {
            var (minLat, maxLat, minLon, maxLon) = GeospatialUtils.CalculateBoundingBox(userLat, userLon, radiusKm);
            query = query.Where(pi =>
                pi.Branch!.Latitude >= minLat && pi.Branch!.Latitude <= maxLat &&
                pi.Branch!.Longitude >= minLon && pi.Branch!.Longitude <= maxLon);
        }

        // 6. Project candidate items
        var now = DateTime.UtcNow;
        var candidates = await query
            .Select(pi => new
            {
                InventoryId = pi.Id,
                pi.MedicineId,
                MedicineBrandName = pi.Medicine!.BrandName,
                MedicineGenericName = pi.Medicine!.GenericName,
                Strength = pi.Medicine!.Strength,
                DosageForm = pi.Medicine!.DosageForm,
                pi.PharmacyId,
                PharmacyName = pi.Pharmacy!.Name,
                pi.BranchId,
                BranchName = pi.Branch!.Name,
                BranchAddress = pi.Branch!.Address,
                BranchCity = pi.Branch!.City,
                BranchPhone = pi.Branch!.Phone,
                BranchOpeningHours = pi.Branch!.OpeningHours,
                BranchLatitude = pi.Branch!.Latitude,
                BranchLongitude = pi.Branch!.Longitude,
                pi.AvailableQuantity,
                pi.Price,
                pi.Currency,
                pi.UpdatedAt
            })
            .ToListAsync(ct);

        // 7. Calculate exact Haversine distance and map to DTOs
        var mapped = new List<MedicineAvailabilityDto>(candidates.Count);

        foreach (var c in candidates)
        {
            double? distanceKm = null;
            if (hasCoordinates)
            {
                distanceKm = GeospatialUtils.CalculateDistanceKm(userLat, userLon, c.BranchLatitude, c.BranchLongitude);
                if (distanceKm > radiusKm)
                    continue; // Exclude candidates outside exact circle radius
            }

            var stockStatus = c.AvailableQuantity >= 5 ? "InStock" : "LowStock";
            var freshness = CalculateFreshness(c.UpdatedAt, now).ToString();

            mapped.Add(new MedicineAvailabilityDto(
                c.InventoryId,
                c.MedicineId,
                c.MedicineBrandName,
                c.MedicineGenericName,
                c.Strength,
                c.DosageForm,
                c.PharmacyId,
                c.PharmacyName,
                c.BranchId,
                c.BranchName,
                c.BranchAddress,
                c.BranchCity,
                c.BranchPhone,
                c.BranchOpeningHours,
                c.BranchLatitude,
                c.BranchLongitude,
                distanceKm,
                c.AvailableQuantity,
                stockStatus,
                c.Price,
                c.Currency,
                freshness,
                c.UpdatedAt));
        }

        // 8. Sorting & Ranking
        IEnumerable<MedicineAvailabilityDto> sorted;
        var sortBy = request.EffectiveSortBy?.Trim().ToLowerInvariant();

        if (hasCoordinates)
        {
            sorted = sortBy switch
            {
                "price" => mapped.OrderBy(m => m.Price).ThenBy(m => m.DistanceKm).ThenBy(m => m.InventoryId),
                "freshness" => mapped.OrderByDescending(m => m.LastUpdatedAt).ThenBy(m => m.DistanceKm).ThenBy(m => m.InventoryId),
                "stock" => mapped.OrderByDescending(m => m.AvailableQuantity).ThenBy(m => m.DistanceKm).ThenBy(m => m.InventoryId),
                _ => mapped.OrderBy(m => m.DistanceKm).ThenByDescending(m => m.LastUpdatedAt).ThenBy(m => m.Price).ThenBy(m => m.InventoryId)
            };
        }
        else
        {
            sorted = sortBy switch
            {
                "price" => mapped.OrderBy(m => m.Price).ThenByDescending(m => m.AvailableQuantity).ThenBy(m => m.InventoryId),
                "freshness" => mapped.OrderByDescending(m => m.LastUpdatedAt).ThenByDescending(m => m.AvailableQuantity).ThenBy(m => m.InventoryId),
                _ => mapped.OrderByDescending(m => m.AvailableQuantity).ThenByDescending(m => m.LastUpdatedAt).ThenBy(m => m.Price).ThenBy(m => m.InventoryId)
            };
        }

        // 9. Pagination
        var sortedList = sorted.ToList();
        var totalCount = sortedList.Count;
        var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
        var pagedItems = sortedList.Skip((page - 1) * pageSize).Take(pageSize).ToList();

        return new AvailabilitySearchResult(
            pagedItems,
            page,
            pageSize,
            totalCount,
            totalPages,
            HasPreviousPage: page > 1,
            HasNextPage: page < totalPages);
    }

    private static StockFreshness CalculateFreshness(DateTime updatedAt, DateTime now)
    {
        var elapsed = now - updatedAt;
        if (elapsed < TimeSpan.Zero) elapsed = TimeSpan.Zero;

        if (elapsed <= TimeSpan.FromMinutes(15))
            return StockFreshness.Fresh;
        if (elapsed <= TimeSpan.FromHours(2))
            return StockFreshness.RecentlyUpdated;
        if (elapsed <= TimeSpan.FromHours(24))
            return StockFreshness.Stale;

        return StockFreshness.Outdated;
    }
}
