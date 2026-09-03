using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using PharmacyAvailability.Application.Catalog;
using PharmacyAvailability.Contracts.Catalog;
using PharmacyAvailability.Domain.Catalog;
using PharmacyAvailability.Domain.Common.Exceptions;
using PharmacyAvailability.Infrastructure.Persistence;

namespace PharmacyAvailability.Infrastructure.Catalog;

public class CatalogService : ICatalogService
{
    private readonly PharmacyDbContext _context;
    private readonly ILogger<CatalogService> _logger;

    public CatalogService(PharmacyDbContext context, ILogger<CatalogService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<PagedResult<MedicineDto>> SearchMedicinesAsync(
        string? searchTerm,
        Guid? categoryId,
        Guid? manufacturerId,
        bool activeOnly = true,
        int page = 1,
        int pageSize = 20,
        string? sortBy = null,
        string? sortOrder = null,
        CancellationToken ct = default)
    {
        // Bound and validate pagination parameters
        page = page < 1 ? 1 : page;
        pageSize = pageSize < 1 ? 20 : (pageSize > 100 ? 100 : pageSize);

        var query = _context.Medicines
            .AsNoTracking()
            .Include(m => m.Category)
            .Include(m => m.Manufacturer)
            .Include(m => m.Barcodes)
            .Include(m => m.Aliases)
            .AsQueryable();

        if (activeOnly)
        {
            query = query.Where(m => m.IsActive);
        }

        if (categoryId.HasValue)
        {
            query = query.Where(m => m.CategoryId == categoryId.Value);
        }

        if (manufacturerId.HasValue)
        {
            query = query.Where(m => m.ManufacturerId == manufacturerId.Value);
        }

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var term = searchTerm.Trim().ToLower();
            query = query.Where(m =>
                m.BrandName.ToLower().Contains(term) ||
                m.GenericName.ToLower().Contains(term) ||
                m.ActiveIngredient.ToLower().Contains(term));
        }

        // Deterministic sorting (always then by ID to ensure stable pagination)
        query = (sortBy?.ToLowerInvariant(), sortOrder?.ToLowerInvariant()) switch
        {
            ("genericname", "desc") => query.OrderByDescending(m => m.GenericName).ThenBy(m => m.Id),
            ("genericname", _) => query.OrderBy(m => m.GenericName).ThenBy(m => m.Id),
            ("createdat", "desc") => query.OrderByDescending(m => m.CreatedAt).ThenBy(m => m.Id),
            ("createdat", _) => query.OrderBy(m => m.CreatedAt).ThenBy(m => m.Id),
            ("brandname", "desc") => query.OrderByDescending(m => m.BrandName).ThenBy(m => m.Id),
            _ => query.OrderBy(m => m.BrandName).ThenBy(m => m.Id)
        };

        var totalCount = await query.CountAsync(ct);
        var medicines = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        var items = medicines.Select(MapToMedicineDto).ToList();
        var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

        return new PagedResult<MedicineDto>(
            items,
            page,
            pageSize,
            totalCount,
            totalPages,
            HasPreviousPage: page > 1,
            HasNextPage: page < totalPages);
    }

    public async Task<MedicineDto?> GetMedicineByIdAsync(Guid id, CancellationToken ct = default)
    {
        var medicine = await _context.Medicines
            .AsNoTracking()
            .Include(m => m.Category)
            .Include(m => m.Manufacturer)
            .Include(m => m.Barcodes)
            .Include(m => m.Aliases)
            .FirstOrDefaultAsync(m => m.Id == id, ct);

        return medicine == null ? null : MapToMedicineDto(medicine);
    }

    public async Task<MedicineDto?> GetMedicineByBarcodeAsync(string barcode, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(barcode))
            return null;

        var normalizedBarcode = barcode.Trim().ToUpperInvariant();

        var barcodeEntry = await _context.MedicineBarcodes
            .AsNoTracking()
            .FirstOrDefaultAsync(b => b.Barcode == normalizedBarcode, ct);

        if (barcodeEntry == null)
            return null;

        return await GetMedicineByIdAsync(barcodeEntry.MedicineId, ct);
    }

    public async Task<IReadOnlyList<MedicineDto>> GetMedicinesByAliasAsync(string alias, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(alias))
            return Array.Empty<MedicineDto>();

        var normalizedAlias = alias.Trim();

        var medicineIds = await _context.MedicineAliases
            .AsNoTracking()
            .Where(a => a.Alias.ToLower() == normalizedAlias.ToLower())
            .Select(a => a.MedicineId)
            .Distinct()
            .ToListAsync(ct);

        if (medicineIds.Count == 0)
            return Array.Empty<MedicineDto>();

        var medicines = await _context.Medicines
            .AsNoTracking()
            .Include(m => m.Category)
            .Include(m => m.Manufacturer)
            .Include(m => m.Barcodes)
            .Include(m => m.Aliases)
            .Where(m => medicineIds.Contains(m.Id))
            .OrderBy(m => m.BrandName)
            .ToListAsync(ct);

        return medicines.Select(MapToMedicineDto).ToList();
    }

    public async Task<IReadOnlyList<CategoryDto>> GetCategoriesAsync(bool activeOnly = true, CancellationToken ct = default)
    {
        var query = _context.Categories.AsNoTracking().AsQueryable();
        if (activeOnly)
            query = query.Where(c => c.IsActive);

        var categories = await query.OrderBy(c => c.Name).ToListAsync(ct);
        return categories.Select(c => new CategoryDto(c.Id, c.Name, c.Description, c.IsActive)).ToList();
    }

    public async Task<IReadOnlyList<ManufacturerDto>> GetManufacturersAsync(bool activeOnly = true, CancellationToken ct = default)
    {
        var query = _context.Manufacturers.AsNoTracking().AsQueryable();
        if (activeOnly)
            query = query.Where(m => m.IsActive);

        var manufacturers = await query.OrderBy(m => m.Name).ToListAsync(ct);
        return manufacturers.Select(m => new ManufacturerDto(m.Id, m.Name, m.Country, m.IsActive)).ToList();
    }

    public async Task<MedicineDto> CreateMedicineAsync(CreateMedicineRequest request, CancellationToken ct = default)
    {
        var categoryExists = await _context.Categories.AnyAsync(c => c.Id == request.CategoryId, ct);
        if (!categoryExists)
            throw new KeyNotFoundException($"Category with ID '{request.CategoryId}' was not found.");

        var manufacturerExists = await _context.Manufacturers.AnyAsync(m => m.Id == request.ManufacturerId, ct);
        if (!manufacturerExists)
            throw new KeyNotFoundException($"Manufacturer with ID '{request.ManufacturerId}' was not found.");

        if (!string.IsNullOrWhiteSpace(request.InitialBarcode))
        {
            var normBarcode = request.InitialBarcode.Trim().ToUpperInvariant();
            var barcodeInUse = await _context.MedicineBarcodes.AnyAsync(b => b.Barcode == normBarcode, ct);
            if (barcodeInUse)
                throw new DomainValidationException($"Barcode '{normBarcode}' is already assigned to another medicine.");
        }

        var medicine = new Medicine(
            request.BrandName,
            request.GenericName,
            request.ActiveIngredient,
            request.Strength,
            request.DosageForm,
            request.CategoryId,
            request.ManufacturerId,
            description: null,
            requiresPrescription: request.RequiresPrescription);

        if (!string.IsNullOrWhiteSpace(request.InitialBarcode))
        {
            medicine.AddBarcode(request.InitialBarcode.Trim().ToUpperInvariant(), isPrimary: true);
        }

        _context.Medicines.Add(medicine);
        await _context.SaveChangesAsync(ct);

        _logger.LogInformation("Medicine created: {MedicineId} ({BrandName})", medicine.Id, medicine.BrandName);

        // Reload with navigations
        return (await GetMedicineByIdAsync(medicine.Id, ct))!;
    }

    public async Task<MedicineDto> UpdateMedicineAsync(Guid id, UpdateMedicineRequest request, CancellationToken ct = default)
    {
        var medicine = await _context.Medicines.FindAsync(new object[] { id }, ct);
        if (medicine == null)
            throw new KeyNotFoundException($"Medicine with ID '{id}' was not found.");

        var categoryExists = await _context.Categories.AnyAsync(c => c.Id == request.CategoryId, ct);
        if (!categoryExists)
            throw new KeyNotFoundException($"Category with ID '{request.CategoryId}' was not found.");

        var manufacturerExists = await _context.Manufacturers.AnyAsync(m => m.Id == request.ManufacturerId, ct);
        if (!manufacturerExists)
            throw new KeyNotFoundException($"Manufacturer with ID '{request.ManufacturerId}' was not found.");

        medicine.UpdateDetails(
            request.BrandName,
            request.GenericName,
            request.ActiveIngredient,
            request.Strength,
            request.DosageForm,
            request.CategoryId,
            request.ManufacturerId,
            request.RequiresPrescription);

        await _context.SaveChangesAsync(ct);
        _logger.LogInformation("Medicine updated: {MedicineId}", id);

        return (await GetMedicineByIdAsync(id, ct))!;
    }

    public async Task<MedicineDto> UpdateMedicineStatusAsync(Guid id, UpdateMedicineStatusRequest request, CancellationToken ct = default)
    {
        var medicine = await _context.Medicines.FindAsync(new object[] { id }, ct);
        if (medicine == null)
            throw new KeyNotFoundException($"Medicine with ID '{id}' was not found.");

        if (request.IsActive)
            medicine.Activate();
        else
            medicine.Deactivate();

        await _context.SaveChangesAsync(ct);
        _logger.LogInformation("Medicine status updated: {MedicineId} (Active: {IsActive})", id, request.IsActive);

        return (await GetMedicineByIdAsync(id, ct))!;
    }

    public async Task<CategoryDto> CreateCategoryAsync(CreateCategoryRequest request, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            throw new DomainValidationException("Category name is required.");

        var category = new Category(request.Name, request.Description);
        _context.Categories.Add(category);
        await _context.SaveChangesAsync(ct);

        _logger.LogInformation("Category created: {CategoryId} ({Name})", category.Id, category.Name);
        return new CategoryDto(category.Id, category.Name, category.Description, category.IsActive);
    }

    public async Task<CategoryDto> UpdateCategoryAsync(Guid id, UpdateCategoryRequest request, CancellationToken ct = default)
    {
        var category = await _context.Categories.FindAsync(new object[] { id }, ct);
        if (category == null)
            throw new KeyNotFoundException($"Category with ID '{id}' was not found.");

        category.Update(request.Name, request.Description);
        if (request.IsActive)
            category.Activate();
        else
            category.Deactivate();

        await _context.SaveChangesAsync(ct);
        return new CategoryDto(category.Id, category.Name, category.Description, category.IsActive);
    }

    public async Task<ManufacturerDto> CreateManufacturerAsync(CreateManufacturerRequest request, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            throw new DomainValidationException("Manufacturer name is required.");

        var manufacturer = new Manufacturer(request.Name, request.Country);
        _context.Manufacturers.Add(manufacturer);
        await _context.SaveChangesAsync(ct);

        _logger.LogInformation("Manufacturer created: {ManufacturerId} ({Name})", manufacturer.Id, manufacturer.Name);
        return new ManufacturerDto(manufacturer.Id, manufacturer.Name, manufacturer.Country, manufacturer.IsActive);
    }

    public async Task<ManufacturerDto> UpdateManufacturerAsync(Guid id, UpdateManufacturerRequest request, CancellationToken ct = default)
    {
        var manufacturer = await _context.Manufacturers.FindAsync(new object[] { id }, ct);
        if (manufacturer == null)
            throw new KeyNotFoundException($"Manufacturer with ID '{id}' was not found.");

        manufacturer.Update(request.Name, request.Country);
        if (request.IsActive)
            manufacturer.Activate();
        else
            manufacturer.Deactivate();

        await _context.SaveChangesAsync(ct);
        return new ManufacturerDto(manufacturer.Id, manufacturer.Name, manufacturer.Country, manufacturer.IsActive);
    }

    public async Task<BarcodeDto> AddBarcodeAsync(Guid medicineId, AddBarcodeRequest request, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(request.Barcode))
            throw new DomainValidationException("Barcode is required.");

        var norm = request.Barcode.Trim().ToUpperInvariant();

        var exists = await _context.MedicineBarcodes.AnyAsync(b => b.Barcode == norm, ct);
        if (exists)
            throw new DomainValidationException($"Barcode '{norm}' is already in use by another medicine.");

        var medicine = await _context.Medicines
            .Include(m => m.Barcodes)
            .FirstOrDefaultAsync(m => m.Id == medicineId, ct);

        if (medicine == null)
            throw new KeyNotFoundException($"Medicine with ID '{medicineId}' was not found.");

        var barcode = medicine.AddBarcode(norm, request.IsPrimary);
        await _context.SaveChangesAsync(ct);

        _logger.LogInformation("Barcode added: {Barcode} to medicine {MedicineId}", norm, medicineId);
        return new BarcodeDto(barcode.Id, barcode.Barcode, barcode.IsPrimary);
    }

    public async Task<AliasDto> AddAliasAsync(Guid medicineId, AddAliasRequest request, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(request.Alias))
            throw new DomainValidationException("Alias is required.");

        var norm = request.Alias.Trim();

        var medicine = await _context.Medicines
            .Include(m => m.Aliases)
            .FirstOrDefaultAsync(m => m.Id == medicineId, ct);

        if (medicine == null)
            throw new KeyNotFoundException($"Medicine with ID '{medicineId}' was not found.");

        if (medicine.Aliases.Any(a => string.Equals(a.Alias, norm, StringComparison.OrdinalIgnoreCase)))
            throw new DomainValidationException($"Alias '{norm}' already exists for this medicine.");

        var alias = medicine.AddAlias(norm);
        await _context.SaveChangesAsync(ct);

        _logger.LogInformation("Alias added: '{Alias}' to medicine {MedicineId}", norm, medicineId);
        return new AliasDto(alias.Id, alias.Alias);
    }

    private static MedicineDto MapToMedicineDto(Medicine m) =>
        new(
            m.Id,
            m.BrandName,
            m.GenericName,
            m.ActiveIngredient,
            m.Strength,
            m.DosageForm,
            m.CategoryId,
            m.Category?.Name ?? string.Empty,
            m.ManufacturerId,
            m.Manufacturer?.Name ?? string.Empty,
            m.RequiresPrescription,
            m.IsActive,
            m.Barcodes.Select(b => new BarcodeDto(b.Id, b.Barcode, b.IsPrimary)).ToList(),
            m.Aliases.Select(a => new AliasDto(a.Id, a.Alias)).ToList(),
            m.CreatedAt);
}
