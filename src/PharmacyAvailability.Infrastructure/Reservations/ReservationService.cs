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

        // A fresh registration cannot be used to reserve stock until both ownership
        // channels have been verified. This prevents anonymous sign-up automation
        // from exhausting pharmacy inventory holds.
        var customer = await _context.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == customerId, ct);

        if (customer == null || !customer.IsActive || customer.Role != UserRole.Customer || !customer.IsFullyVerified)
            throw new UnauthorizedAccessException("A fully verified customer account is required to place a reservation.");

        // Explicit database transaction guarantees full rollback on any failure
        using var transaction = await _context.Database.BeginTransactionAsync(ct);

        try
        {
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

            // 3. Load branch inventory (tracked with RowVersion concurrency token)
            var inventory = await _context.PharmacyInventories
                .FirstOrDefaultAsync(pi => pi.BranchId == request.BranchId && pi.MedicineId == request.MedicineId, ct);

            if (inventory == null)
                throw new KeyNotFoundException($"No inventory found for medicine '{medicine.BrandName}' at branch '{branch.Name}'.");

            // 4. Reserve stock atomically on inventory aggregate (invariants checked)
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
            await transaction.CommitAsync(ct);

            _logger.LogInformation(
                "Reservation {ReservationId} created for customer {CustomerId} at branch {BranchId} (Qty: {Qty})",
                reservation.Id, customerId, branch.Id, request.Quantity);

            return MapToDto(reservation, branch.Pharmacy.Name, branch.Name, branch.Address, medicine.BrandName, medicine.GenericName);
        }
        catch
        {
            await transaction.RollbackAsync(ct);
            throw;
        }
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

        // Check if reservation is overdue: inline expiration if past ExpiresAt
        if (reservation.Status == ReservationStatus.Active && reservation.ExpiresAt <= DateTime.UtcNow)
        {
            using var expireTx = await _context.Database.BeginTransactionAsync(ct);
            reservation.Expire();
            if (inventory.ReservedQuantity >= reservation.Quantity)
            {
                inventory.ReleaseReservation(
                    reservation.Quantity,
                    customerId,
                    $"Reservation {reservation.Id} expired automatically at cancellation attempt",
                    InventoryAdjustmentReason.ReservationExpired);
            }
            else if (inventory.ReservedQuantity > 0)
            {
                inventory.ReleaseReservation(
                    inventory.ReservedQuantity,
                    customerId,
                    $"Reservation {reservation.Id} expired automatically at cancellation attempt",
                    InventoryAdjustmentReason.ReservationExpired);
            }
            await _context.SaveChangesAsync(ct);
            await expireTx.CommitAsync(ct);

            throw new ResourceConflictException($"Cannot cancel reservation '{reservation.Id}' because it already expired at {reservation.ExpiresAt:u}.");
        }

        // Transitions to Cancelled or throws ResourceConflictException if not Active
        using var transaction = await _context.Database.BeginTransactionAsync(ct);
        try
        {
            reservation.Cancel();

            inventory.ReleaseReservation(
                reservation.Quantity,
                customerId,
                $"Reservation {reservation.Id} cancelled by customer",
                InventoryAdjustmentReason.ReservationRelease);

            await _context.SaveChangesAsync(ct);
            await transaction.CommitAsync(ct);

            _logger.LogInformation(
                "Reservation {ReservationId} cancelled by customer {CustomerId}",
                reservation.Id, customerId);

            return MapToDto(reservation);
        }
        catch
        {
            await transaction.RollbackAsync(ct);
            throw;
        }
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

        // Check if reservation is overdue: inline expiration if past ExpiresAt
        if (reservation.Status == ReservationStatus.Active && reservation.ExpiresAt <= DateTime.UtcNow)
        {
            using var expireTx = await _context.Database.BeginTransactionAsync(ct);
            reservation.Expire();
            if (inventory.ReservedQuantity >= reservation.Quantity)
            {
                inventory.ReleaseReservation(
                    reservation.Quantity,
                    staffUserId,
                    $"Reservation {reservation.Id} expired automatically at confirmation attempt",
                    InventoryAdjustmentReason.ReservationExpired);
            }
            else if (inventory.ReservedQuantity > 0)
            {
                inventory.ReleaseReservation(
                    inventory.ReservedQuantity,
                    staffUserId,
                    $"Reservation {reservation.Id} expired automatically at confirmation attempt",
                    InventoryAdjustmentReason.ReservationExpired);
            }
            await _context.SaveChangesAsync(ct);
            await expireTx.CommitAsync(ct);

            throw new ResourceConflictException($"Cannot confirm reservation '{reservation.Id}' because it expired at {reservation.ExpiresAt:u}.");
        }

        // Transitions to Confirmed or throws ResourceConflictException if not Active
        using var transaction = await _context.Database.BeginTransactionAsync(ct);
        try
        {
            reservation.Confirm();

            inventory.ConfirmReservation(
                reservation.Quantity,
                staffUserId,
                $"Reservation {reservation.Id} confirmed by staff");

            await _context.SaveChangesAsync(ct);
            await transaction.CommitAsync(ct);

            _logger.LogInformation(
                "Reservation {ReservationId} confirmed by staff {StaffUserId}",
                reservation.Id, staffUserId);

            return MapToDto(reservation);
        }
        catch
        {
            await transaction.RollbackAsync(ct);
            throw;
        }
    }

    public async Task<ReservationDto> ReleaseReservationByAdminAsync(
        Guid reservationId,
        Guid adminUserId,
        string reason,
        CancellationToken ct = default)
    {
        var reservation = await _context.Reservations
            .Include(r => r.Pharmacy)
            .Include(r => r.Branch)
            .Include(r => r.Medicine)
            .FirstOrDefaultAsync(r => r.Id == reservationId, ct);

        if (reservation == null)
            throw new KeyNotFoundException($"Reservation with ID '{reservationId}' was not found.");

        var inventory = await _context.PharmacyInventories
            .FirstOrDefaultAsync(pi => pi.BranchId == reservation.BranchId && pi.MedicineId == reservation.MedicineId, ct);

        if (inventory == null)
            throw new KeyNotFoundException($"Inventory item for branch '{reservation.BranchId}' was not found.");

        // Check if reservation is overdue: inline expiration if past ExpiresAt
        if (reservation.Status == ReservationStatus.Active && reservation.ExpiresAt <= DateTime.UtcNow)
        {
            using var expireTx = await _context.Database.BeginTransactionAsync(ct);
            reservation.Expire();
            if (inventory.ReservedQuantity >= reservation.Quantity)
            {
                inventory.ReleaseReservation(
                    reservation.Quantity,
                    adminUserId,
                    $"Reservation {reservation.Id} expired automatically at admin release attempt",
                    InventoryAdjustmentReason.ReservationExpired);
            }
            else if (inventory.ReservedQuantity > 0)
            {
                inventory.ReleaseReservation(
                    inventory.ReservedQuantity,
                    adminUserId,
                    $"Reservation {reservation.Id} expired automatically at admin release attempt",
                    InventoryAdjustmentReason.ReservationExpired);
            }
            await _context.SaveChangesAsync(ct);
            await expireTx.CommitAsync(ct);

            throw new ResourceConflictException($"Cannot release reservation '{reservation.Id}' because it already expired at {reservation.ExpiresAt:u}.");
        }

        using var transaction = await _context.Database.BeginTransactionAsync(ct);
        try
        {
            // Transition to Released
            reservation.Release();

            inventory.ReleaseReservation(
                reservation.Quantity,
                adminUserId,
                $"Administrative release by platform admin: {reason}",
                InventoryAdjustmentReason.ManualAdjustment);

            await _context.SaveChangesAsync(ct);
            await transaction.CommitAsync(ct);

            _logger.LogInformation(
                "Reservation {ReservationId} administratively released by admin {AdminUserId}: {Reason}",
                reservation.Id, adminUserId, reason);

            return MapToDto(reservation);
        }
        catch
        {
            await transaction.RollbackAsync(ct);
            throw;
        }
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
                using var tx = await _context.Database.BeginTransactionAsync(ct);

                r.Expire();

                var inventory = await _context.PharmacyInventories
                    .FirstOrDefaultAsync(pi => pi.BranchId == r.BranchId && pi.MedicineId == r.MedicineId, ct);

                if (inventory != null)
                {
                    if (inventory.ReservedQuantity >= r.Quantity)
                    {
                        inventory.ReleaseReservation(
                            r.Quantity,
                            systemUser.Id,
                            $"Reservation {r.Id} expired automatically by system",
                            InventoryAdjustmentReason.ReservationExpired);
                    }
                    else if (inventory.ReservedQuantity > 0)
                    {
                        var actualRelease = inventory.ReservedQuantity;
                        inventory.ReleaseReservation(
                            actualRelease,
                            systemUser.Id,
                            $"Reservation {r.Id} expired (partial release {actualRelease} of {r.Quantity} due to inventory discrepancy)",
                            InventoryAdjustmentReason.ReservationExpired);

                        _logger.LogWarning(
                            "Reservation {ReservationId} requested release of {Quantity} units, but only {ReservedQuantity} was reserved in inventory {InventoryId}. Partial release executed.",
                            r.Id, r.Quantity, actualRelease, inventory.Id);
                    }
                    else
                    {
                        _logger.LogWarning(
                            "Reservation {ReservationId} expired with quantity {Quantity}, but inventory {InventoryId} has 0 reserved units. Reservation marked Expired without inventory release.",
                            r.Id, r.Quantity, inventory.Id);
                    }
                }
                else
                {
                    _logger.LogWarning(
                        "Reservation {ReservationId} expired with quantity {Quantity}, but inventory for Branch {BranchId} and Medicine {MedicineId} was not found. Reservation marked Expired.",
                        r.Id, r.Quantity, r.BranchId, r.MedicineId);
                }

                await _context.SaveChangesAsync(ct);
                await tx.CommitAsync(ct);
                expiredCount++;
            }
            catch (Exception ex)
            {
                // Failure isolation: detach dirty state so other reservations in batch proceed unaffected
                _context.Entry(r).State = EntityState.Unchanged;
                var dirtyInv = _context.ChangeTracker.Entries<PharmacyInventory>()
                    .FirstOrDefault(e => e.Entity.BranchId == r.BranchId && e.Entity.MedicineId == r.MedicineId);
                if (dirtyInv != null)
                    dirtyInv.State = EntityState.Unchanged;

                _logger.LogWarning(ex, "Reservation {ReservationId} had error/concurrency conflict during expiration sweep. Skipped.", r.Id);
            }
        }

        if (expiredCount > 0)
        {
            _logger.LogInformation("Expired {Count} overdue reservations.", expiredCount);
        }

        return expiredCount;
    }

    private static ReservationDto MapToDto(Reservation r) =>
        MapToDto(
            r,
            r.Pharmacy?.Name ?? string.Empty,
            r.Branch?.Name ?? string.Empty,
            r.Branch?.Address ?? string.Empty,
            r.Medicine?.BrandName ?? string.Empty,
            r.Medicine?.GenericName ?? string.Empty);

    private static ReservationDto MapToDto(
        Reservation r,
        string pharmacyName,
        string branchName,
        string branchAddress,
        string medicineBrandName,
        string medicineGenericName) =>
        new(
            r.Id,
            r.CustomerId,
            r.PharmacyId,
            pharmacyName,
            r.BranchId,
            branchName,
            branchAddress,
            r.MedicineId,
            medicineBrandName,
            medicineGenericName,
            r.Quantity,
            r.Status.ToString(),
            r.CreatedAt,
            r.ExpiresAt,
            r.ConfirmedAt,
            r.ReleasedAt,
            r.CancelledAt,
            r.ExpiredAt);
}
