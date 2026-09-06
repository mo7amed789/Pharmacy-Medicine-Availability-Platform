using Microsoft.EntityFrameworkCore;
using PharmacyAvailability.Application.Operations;
using PharmacyAvailability.Contracts.Catalog;
using PharmacyAvailability.Contracts.Operations;
using PharmacyAvailability.Domain.Inventory;
using PharmacyAvailability.Domain.Reservations;
using PharmacyAvailability.Infrastructure.Persistence;

namespace PharmacyAvailability.Infrastructure.Operations;

public class PharmacyOperationsService : IPharmacyOperationsService
{
    private readonly PharmacyDbContext _context;

    public PharmacyOperationsService(PharmacyDbContext context)
    {
        _context = context;
    }

    private async Task<(Domain.Pharmacies.Pharmacy Pharmacy, Domain.Pharmacies.PharmacyBranch Branch)> ValidateBranchAccessAsync(
        Guid pharmacyId,
        Guid branchId,
        CancellationToken ct)
    {
        var branch = await _context.PharmacyBranches
            .Include(b => b.Pharmacy)
            .AsNoTracking()
            .FirstOrDefaultAsync(b => b.Id == branchId &&
                                      b.PharmacyId == pharmacyId &&
                                      b.IsActive &&
                                      b.Pharmacy != null &&
                                      b.Pharmacy.Status == Domain.Pharmacies.PharmacyStatus.Active, ct);

        if (branch == null || branch.Pharmacy == null)
            throw new KeyNotFoundException($"Branch '{branchId}' for pharmacy '{pharmacyId}' was not found.");

        return (branch.Pharmacy, branch);
    }

    public async Task<BranchDashboardDto> GetBranchDashboardAsync(
        Guid pharmacyId,
        Guid branchId,
        CancellationToken ct = default)
    {
        var (pharmacy, branch) = await ValidateBranchAccessAsync(pharmacyId, branchId, ct);

        var now = DateTime.UtcNow;
        var startOfTodayUtc = DateTime.UtcNow.Date;
        var startOfTomorrowUtc = startOfTodayUtc.AddDays(1);
        var soonThreshold = now.AddMinutes(30);

        var activeCount = await _context.Reservations
            .CountAsync(r => r.BranchId == branchId &&
                             r.Status == ReservationStatus.Active &&
                             r.ExpiresAt > now, ct);

        var expiringSoonCount = await _context.Reservations
            .CountAsync(r => r.BranchId == branchId &&
                             r.Status == ReservationStatus.Active &&
                             r.ExpiresAt <= soonThreshold &&
                             r.ExpiresAt > now, ct);

        var confirmedToday = await _context.Reservations
            .CountAsync(r => r.BranchId == branchId &&
                             r.Status == ReservationStatus.Confirmed &&
                             r.ConfirmedAt >= startOfTodayUtc &&
                             r.ConfirmedAt < startOfTomorrowUtc, ct);

        var cancelledToday = await _context.Reservations
            .CountAsync(r => r.BranchId == branchId &&
                             r.Status == ReservationStatus.Cancelled &&
                             r.CancelledAt >= startOfTodayUtc &&
                             r.CancelledAt < startOfTomorrowUtc, ct);

        var expiredToday = await _context.Reservations
            .CountAsync(r => r.BranchId == branchId &&
                             r.Status == ReservationStatus.Expired &&
                             r.ExpiredAt >= startOfTodayUtc &&
                             r.ExpiredAt < startOfTomorrowUtc, ct);

        var lowStockCount = await _context.PharmacyInventories
            .CountAsync(pi => pi.BranchId == branchId &&
                              pi.Medicine != null &&
                              pi.Medicine.IsActive &&
                              pi.AvailableQuantity <= 5, ct);

        var totalActiveMedicines = await _context.PharmacyInventories
            .CountAsync(pi => pi.BranchId == branchId &&
                              pi.Medicine != null &&
                              pi.Medicine.IsActive &&
                              pi.AvailableQuantity > 0, ct);

        return new BranchDashboardDto(
            PharmacyId: pharmacy.Id,
            PharmacyName: pharmacy.Name,
            BranchId: branch.Id,
            BranchName: branch.Name,
            ActiveReservationsCount: activeCount,
            ExpiringSoonReservationsCount: expiringSoonCount,
            TodayConfirmedPickupsCount: confirmedToday,
            TodayCancelledReservationsCount: cancelledToday,
            TodayExpiredReservationsCount: expiredToday,
            LowStockItemsCount: lowStockCount,
            TotalActiveMedicinesCount: totalActiveMedicines,
            GeneratedAtUtc: now);
    }

    public async Task<PagedResult<StaffReservationQueueItemDto>> GetStaffReservationQueueAsync(
        Guid pharmacyId,
        Guid branchId,
        StaffReservationQueueQuery query,
        CancellationToken ct = default)
    {
        await ValidateBranchAccessAsync(pharmacyId, branchId, ct);

        var now = DateTime.UtcNow;
        var page = Math.Max(1, query.Page);
        var pageSize = Math.Clamp(query.PageSize, 1, 100);

        var q = _context.Reservations
            .AsNoTracking()
            .Where(r => r.BranchId == branchId);

        // Status filter
        if (!string.IsNullOrWhiteSpace(query.Status) &&
            Enum.TryParse<ReservationStatus>(query.Status, true, out var st))
        {
            if (st == ReservationStatus.Active)
            {
                // Active queue strictly displays non-overdue active holds
                q = q.Where(r => r.Status == ReservationStatus.Active && r.ExpiresAt > now);
            }
            else
            {
                q = q.Where(r => r.Status == st);
            }
        }

        // Search term (evaluated sargable in SQL Server without LOWER())
        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var term = query.Search.Trim();
            if (Guid.TryParse(term, out var resGuid))
            {
                q = q.Where(r => r.Id == resGuid);
            }
            else
            {
                q = q.Where(r =>
                    (r.Customer != null && (
                        r.Customer.FullName.Contains(term) ||
                        (r.Customer.PhoneNumber != null && r.Customer.PhoneNumber.Contains(term)) ||
                        r.Customer.Email.Contains(term))) ||
                    (r.Medicine != null && r.Medicine.BrandName.Contains(term)));
            }
        }

        // Expiring soon filter (strictly Active, not yet expired, expiring within 30m)
        if (query.ExpiringSoonOnly == true)
        {
            var soon = now.AddMinutes(30);
            q = q.Where(r => r.Status == ReservationStatus.Active && r.ExpiresAt <= soon && r.ExpiresAt > now);
        }

        var totalCount = await q.CountAsync(ct);
        var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

        // Join with branch inventory for current unit price and project needed fields with deterministic tie-breaker
        var rawItems = await (from r in q
                              join inv in _context.PharmacyInventories.Where(pi => pi.BranchId == branchId)
                              on r.MedicineId equals inv.MedicineId into invGroup
                              from inv in invGroup.DefaultIfEmpty()
                              orderby r.ExpiresAt ascending, r.CreatedAt descending, r.Id ascending
                              select new
                              {
                                  r.Id,
                                  r.CustomerId,
                                  CustomerName = r.Customer != null ? r.Customer.FullName : string.Empty,
                                  CustomerPhone = r.Customer != null ? (r.Customer.PhoneNumber ?? string.Empty) : string.Empty,
                                  CustomerEmail = r.Customer != null ? r.Customer.Email : string.Empty,
                                  r.MedicineId,
                                  MedicineBrandName = r.Medicine != null ? r.Medicine.BrandName : string.Empty,
                                  MedicineGenericName = r.Medicine != null ? r.Medicine.GenericName : string.Empty,
                                  DosageForm = r.Medicine != null ? r.Medicine.DosageForm : string.Empty,
                                  Strength = r.Medicine != null ? r.Medicine.Strength : string.Empty,
                                  r.Quantity,
                                  UnitPrice = (decimal?)inv.Price ?? 0m,
                                  r.Status,
                                  r.CreatedAt,
                                  r.ExpiresAt
                              })
                              .Skip((page - 1) * pageSize)
                              .Take(pageSize)
                              .ToListAsync(ct);

        var items = rawItems.Select(r =>
        {
            var remainingMinutes = (r.Status == ReservationStatus.Active && r.ExpiresAt > now)
                ? Math.Max(0, (int)Math.Ceiling((r.ExpiresAt - now).TotalMinutes))
                : 0;

            var isExpiringSoon = r.Status == ReservationStatus.Active &&
                                 r.ExpiresAt > now &&
                                 r.ExpiresAt <= now.AddMinutes(30);

            var totalAmountDue = r.UnitPrice * r.Quantity;

            return new StaffReservationQueueItemDto(
                ReservationId: r.Id,
                CustomerId: r.CustomerId,
                CustomerName: r.CustomerName,
                CustomerPhone: r.CustomerPhone,
                CustomerEmail: r.CustomerEmail,
                MedicineId: r.MedicineId,
                MedicineBrandName: r.MedicineBrandName,
                MedicineGenericName: r.MedicineGenericName,
                DosageForm: r.DosageForm,
                Strength: r.Strength,
                Quantity: r.Quantity,
                UnitPrice: r.UnitPrice,
                TotalAmountDue: totalAmountDue,
                Status: r.Status.ToString(),
                CreatedAt: r.CreatedAt,
                ExpiresAt: r.ExpiresAt,
                RemainingMinutes: remainingMinutes,
                IsExpiringSoon: isExpiringSoon);
        }).ToList();

        return new PagedResult<StaffReservationQueueItemDto>(
            items,
            page,
            pageSize,
            totalCount,
            totalPages,
            HasPreviousPage: page > 1,
            HasNextPage: page < totalPages);
    }

    public async Task<PagedResult<LowStockAlertDto>> GetLowStockAlertsAsync(
        Guid pharmacyId,
        Guid branchId,
        int threshold = 5,
        int page = 1,
        int pageSize = 20,
        CancellationToken ct = default)
    {
        await ValidateBranchAccessAsync(pharmacyId, branchId, ct);

        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);
        threshold = Math.Clamp(threshold, 0, 100000);

        var q = _context.PharmacyInventories
            .AsNoTracking()
            .Where(pi => pi.BranchId == branchId &&
                         pi.Medicine != null &&
                         pi.Medicine.IsActive &&
                         pi.AvailableQuantity <= threshold);

        var totalCount = await q.CountAsync(ct);
        var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

        var items = await q
            .OrderBy(pi => pi.AvailableQuantity)
            .ThenBy(pi => pi.Medicine != null ? pi.Medicine.BrandName : string.Empty)
            .ThenBy(pi => pi.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(pi => new LowStockAlertDto(
                pi.Id,
                pi.MedicineId,
                pi.Medicine != null ? pi.Medicine.BrandName : string.Empty,
                pi.Medicine != null ? pi.Medicine.GenericName : string.Empty,
                pi.Medicine != null && pi.Medicine.Category != null ? pi.Medicine.Category.Name : string.Empty,
                pi.Medicine != null && pi.Medicine.Manufacturer != null ? pi.Medicine.Manufacturer.Name : string.Empty,
                pi.AvailableQuantity,
                pi.ReservedQuantity,
                pi.AvailableQuantity + pi.ReservedQuantity,
                pi.Price,
                threshold,
                pi.UpdatedAt))
            .ToListAsync(ct);

        return new PagedResult<LowStockAlertDto>(
            items,
            page,
            pageSize,
            totalCount,
            totalPages,
            HasPreviousPage: page > 1,
            HasNextPage: page < totalPages);
    }

    public async Task<PagedResult<BranchActivityFeedItemDto>> GetBranchActivityFeedAsync(
        Guid pharmacyId,
        Guid branchId,
        int page = 1,
        int pageSize = 20,
        CancellationToken ct = default)
    {
        await ValidateBranchAccessAsync(pharmacyId, branchId, ct);

        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var q = from t in _context.InventoryTransactions.AsNoTracking()
                join inv in _context.PharmacyInventories.AsNoTracking() on t.PharmacyInventoryId equals inv.Id
                join med in _context.Medicines.AsNoTracking() on inv.MedicineId equals med.Id into medGroup
                from med in medGroup.DefaultIfEmpty()
                join u in _context.Users.AsNoTracking() on t.ActorUserId equals u.Id into userGroup
                from u in userGroup.DefaultIfEmpty()
                where inv.BranchId == branchId
                orderby t.CreatedAt descending, t.Id descending
                select new BranchActivityFeedItemDto(
                    t.Id,
                    t.Reason.ToString(),
                    t.Notes ?? t.Reason.ToString(),
                    (Guid?)inv.MedicineId,
                    med != null ? med.BrandName : null,
                    t.DeltaQuantity,
                    t.ResultingQuantity,
                    u != null ? u.Role.ToString() : null,
                    t.CreatedAt);

        var totalCount = await q.CountAsync(ct);
        var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

        var items = await q
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return new PagedResult<BranchActivityFeedItemDto>(
            items,
            page,
            pageSize,
            totalCount,
            totalPages,
            HasPreviousPage: page > 1,
            HasNextPage: page < totalPages);
    }
}
