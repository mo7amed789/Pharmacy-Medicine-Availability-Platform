using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PharmacyAvailability.Application.Reservations;
using PharmacyAvailability.Contracts.Catalog;
using PharmacyAvailability.Contracts.Reservations;
using PharmacyAvailability.Domain.Common.Exceptions;
using PharmacyAvailability.Domain.Identity;
using PharmacyAvailability.Domain.Inventory;
using PharmacyAvailability.Domain.Pharmacies;
using PharmacyAvailability.Domain.Reservations;
using PharmacyAvailability.Infrastructure.Persistence;

namespace PharmacyAvailability.Infrastructure.Reservations;

public class ReservationService : IReservationService
{
    private readonly PharmacyDbContext _context;
    private readonly ReservationOptions _options;
    private readonly ILogger<ReservationService> _logger;

    public ReservationService(
        PharmacyDbContext context,
        IOptions<ReservationOptions> options,
        ILogger<ReservationService> logger)
    {
        _context = context;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<ReservationDto> CreateReservationAsync(
        CreateReservationRequest request,
        Guid customerId,
        CancellationToken ct = default)
    {
        if (customerId == Guid.Empty)
            throw new DomainValidationException("CustomerId is required.");

        if (request.Quantity <= 0)
            throw new DomainValidationException("Reservation quantity must be greater than zero.");

        // 1. Verify branch and pharmacy status
        var branch = await _context.PharmacyBranches
            .Include(b => b.Pharmacy)
            .FirstOrDefaultAsync(b => b.Id == request.BranchId, ct);

        if (branch == null)
            throw new KeyNotFoundException($"Branch with ID '{request.BranchId}' was not found.");

        if (!branch.IsActive)
            throw new DomainValidationException($"Branch '{branch.Name}' is currently inactive.");

        if (branch.Pharmacy == null)
            throw new KeyNotFoundException("Parent pharmacy for this branch was not found.");

        if (branch.Pharmacy.Status != PharmacyStatus.Active)
            throw new DomainValidationException($"Pharmacy '{branch.Pharmacy.Name}' is {branch.Pharmacy.Status} and cannot accept reservations.");

        // 2. Verify medicine status
        var medicine = await _context.Medicines
            .FirstOrDefaultAsync(m => m.Id == request.MedicineId, ct);

        if (medicine == null)
            throw new KeyNotFoundException($"Medicine with ID '{request.MedicineId}' was not found.");

        if (!medicine.IsActive)
            throw new DomainValidationException($"Medicine '{medicine.BrandName}' is currently inactive.");

        // 3. Load branch inventory (tracked for atomic update)
        var inventory = await _context.PharmacyInventories
            .FirstOrDefaultAsync(pi => pi.BranchId == request.BranchId && pi.MedicineId == request.MedicineId, ct);

        if (inventory == null)
            throw new KeyNotFoundException($"No inventory found for medicine '{medicine.BrandName}' at branch '{branch.Name}'.");

        // 4. Reserve stock atomically on inventory aggregate
        inventory.Reserve(
            request.Quantity,
            customerId,
            "Customer reservation hold");

        // 5. Create reservation entity
        var expiresAt = DateTime.UtcNow.AddMinutes(_options.HoldDurationMinutes);
        var reservation = new Reservation(
            customerId,
            branch.PharmacyId,
            branch.Id,
            medicine.Id,
            request.Quantity,
            expiresAt);

        _context.Reservations.Add(reservation);
        await _context.SaveChangesAsync(ct);

        _logger.LogInformation(
            "Reservation {ReservationId} created for customer {CustomerId} at branch {BranchId} (Qty: {Qty})",
            reservation.Id, customerId, branch.Id, request.Quantity);

        return new ReservationDto(
            reservation.Id,
            reservation.CustomerId,
            reservation.PharmacyId,
            branch.Pharmacy.Name,
            reservation.BranchId,
            branch.Name,
            branch.Address,
            reservation.MedicineId,
            medicine.BrandName,
            medicine.GenericName,
            reservation.Quantity,
            reservation.Status.ToString(),
            reservation.CreatedAt,
            reservation.ExpiresAt,
            reservation.ConfirmedAt,
            reservation.ReleasedAt,
            reservation.CancelledAt);
    }

    public async Task<ReservationDto> CancelReservationAsync(
        Guid reservationId,
        Guid customerId,
        CancellationToken ct = default)
    {
        var reservation = await _context.Reservations
            .Include(r => r.Pharmacy)
            .Include(r => r.Branch)
            .Include(r => r.Medicine)
            .FirstOrDefaultAsync(r => r.Id == reservationId, ct);

        if (reservation == null || reservation.CustomerId != customerId)
            throw new KeyNotFoundException($"Reservation with ID '{reservationId}' was not found.");

        var inventory = await _context.PharmacyInventories
            .FirstOrDefaultAsync(pi => pi.BranchId == reservation.BranchId && pi.MedicineId == reservation.MedicineId, ct);

        if (inventory == null)
            throw new KeyNotFoundException($"Inventory item for branch '{reservation.BranchId}' was not found.");

        // Transitions to Cancelled or throws ResourceConflictException if not Active
        reservation.Cancel();

        inventory.ReleaseReservation(
            reservation.Quantity,
            customerId,
            $"Reservation {reservation.Id} cancelled by customer",
            InventoryAdjustmentReason.ReservationRelease);

        await _context.SaveChangesAsync(ct);

        _logger.LogInformation(
            "Reservation {ReservationId} cancelled by customer {CustomerId}",
            reservation.Id, customerId);

        return MapToDto(reservation);
    }

    public async Task<ReservationDto> ConfirmReservationAsync(
        Guid reservationId,
        Guid staffUserId,
        Guid? staffPharmacyId,
        bool isPlatformAdmin,
        CancellationToken ct = default)
    {
        var reservation = await _context.Reservations
            .Include(r => r.Pharmacy)
            .Include(r => r.Branch)
            .Include(r => r.Medicine)
            .FirstOrDefaultAsync(r => r.Id == reservationId, ct);

        if (reservation == null)
            throw new KeyNotFoundException($"Reservation with ID '{reservationId}' was not found.");

        if (!isPlatformAdmin && (!staffPharmacyId.HasValue || staffPharmacyId.Value != reservation.PharmacyId))
            throw new UnauthorizedAccessException("You are not authorized to confirm reservations for another pharmacy.");

        var inventory = await _context.PharmacyInventories
            .FirstOrDefaultAsync(pi => pi.BranchId == reservation.BranchId && pi.MedicineId == reservation.MedicineId, ct);

        if (inventory == null)
            throw new KeyNotFoundException($"Inventory item for branch '{reservation.BranchId}' was not found.");

        // Transitions to Confirmed or throws ResourceConflictException if not Active
        reservation.Confirm();

        inventory.ConfirmReservation(
            reservation.Quantity,
            staffUserId,
            $"Reservation {reservation.Id} confirmed by staff");

        await _context.SaveChangesAsync(ct);

        _logger.LogInformation(
            "Reservation {ReservationId} confirmed by staff {StaffUserId}",
            reservation.Id, staffUserId);

        return MapToDto(reservation);
    }

    public async Task<PagedResult<ReservationDto>> GetReservationsAsync(
        GetReservationsQuery query,
        Guid userId,
        UserRole role,
        Guid? userPharmacyId,
        CancellationToken ct = default)
    {
        var page = Math.Max(1, query.Page);
        var pageSize = Math.Clamp(query.PageSize, 1, 100);

        var q = _context.Reservations
            .AsNoTracking()
            .Include(r => r.Pharmacy)
            .Include(r => r.Branch)
            .Include(r => r.Medicine)
            .AsQueryable();

        // Multi-tenant scoping
        if (role == UserRole.Customer)
        {
            q = q.Where(r => r.CustomerId == userId);
        }
        else if (role is UserRole.PharmacyStaff or UserRole.PharmacyAdmin)
        {
            if (!userPharmacyId.HasValue)
            {
                return new PagedResult<ReservationDto>(
                    Array.Empty<ReservationDto>(), page, pageSize, 0, 0, false, false);
            }

            q = q.Where(r => r.PharmacyId == userPharmacyId.Value);
        }
        else if (role == UserRole.PlatformAdmin)
        {
            // Global admin can view all
        }
        else
        {
            return new PagedResult<ReservationDto>(
                Array.Empty<ReservationDto>(), page, pageSize, 0, 0, false, false);
        }

        // Status filter
        if (!string.IsNullOrWhiteSpace(query.Status) &&
            Enum.TryParse<ReservationStatus>(query.Status, true, out var status))
        {
            q = q.Where(r => r.Status == status);
        }

        var totalCount = await q.CountAsync(ct);
        var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

        var items = await q
            .OrderByDescending(r => r.CreatedAt)
            .ThenBy(r => r.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return new PagedResult<ReservationDto>(
            items.Select(MapToDto).ToList(),
            page,
            pageSize,
            totalCount,
            totalPages,
            HasPreviousPage: page > 1,
            HasNextPage: page < totalPages);
    }

    public async Task<ReservationDto?> GetReservationByIdAsync(
        Guid reservationId,
        Guid userId,
        UserRole role,
        Guid? userPharmacyId,
        CancellationToken ct = default)
    {
        var reservation = await _context.Reservations
            .AsNoTracking()
            .Include(r => r.Pharmacy)
            .Include(r => r.Branch)
            .Include(r => r.Medicine)
            .FirstOrDefaultAsync(r => r.Id == reservationId, ct);

        if (reservation == null)
            return null;

        // Security / IDOR validation
        if (role == UserRole.Customer && reservation.CustomerId != userId)
            return null;

        if (role is UserRole.PharmacyStaff or UserRole.PharmacyAdmin &&
            (!userPharmacyId.HasValue || reservation.PharmacyId != userPharmacyId.Value))
            return null;

        return MapToDto(reservation);
    }

    public async Task<int> ExpireOverdueReservationsAsync(CancellationToken ct = default)
    {
        var now = DateTime.UtcNow;

        var overdue = await _context.Reservations
            .Where(r => r.Status == ReservationStatus.Active && r.ExpiresAt <= now)
            .OrderBy(r => r.ExpiresAt)
            .Take(50)
            .ToListAsync(ct);

        if (overdue.Count == 0)
            return 0;

        // Obtain an existing platform admin user as actor
        var systemUser = await _context.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Role == UserRole.PlatformAdmin, ct)
            ?? await _context.Users.AsNoTracking().FirstAsync(ct);

        int expiredCount = 0;

        foreach (var r in overdue)
        {
            try
            {
                var inventory = await _context.PharmacyInventories
                    .FirstOrDefaultAsync(pi => pi.BranchId == r.BranchId && pi.MedicineId == r.MedicineId, ct);

                if (inventory != null)
                {
                    r.Expire();

                    inventory.ReleaseReservation(
                        r.Quantity,
                        systemUser.Id,
                        $"Reservation {r.Id} expired automatically by system",
                        InventoryAdjustmentReason.ReservationExpired);

                    await _context.SaveChangesAsync(ct);
                    expiredCount++;
                }
            }
            catch (Exception ex) when (ex is DbUpdateConcurrencyException or ResourceConflictException)
            {
                _logger.LogWarning(ex, "Reservation {ReservationId} had concurrent modification during expiration sweep.", r.Id);
            }
        }

        if (expiredCount > 0)
        {
            _logger.LogInformation("Expired {Count} overdue reservations.", expiredCount);
        }

        return expiredCount;
    }

    private static ReservationDto MapToDto(Reservation r) =>
        new(
            r.Id,
            r.CustomerId,
            r.PharmacyId,
            r.Pharmacy?.Name ?? string.Empty,
            r.BranchId,
            r.Branch?.Name ?? string.Empty,
            r.Branch?.Address ?? string.Empty,
            r.MedicineId,
            r.Medicine?.BrandName ?? string.Empty,
            r.Medicine?.GenericName ?? string.Empty,
            r.Quantity,
            r.Status.ToString(),
            r.CreatedAt,
            r.ExpiresAt,
            r.ConfirmedAt,
            r.ReleasedAt,
            r.CancelledAt);
}
