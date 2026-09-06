using PharmacyAvailability.Contracts.Catalog;
using PharmacyAvailability.Contracts.Operations;

namespace PharmacyAvailability.Application.Operations;

public interface IPharmacyOperationsService
{
    Task<BranchDashboardDto> GetBranchDashboardAsync(
        Guid pharmacyId,
        Guid branchId,
        CancellationToken ct = default);

    Task<PagedResult<StaffReservationQueueItemDto>> GetStaffReservationQueueAsync(
        Guid pharmacyId,
        Guid branchId,
        StaffReservationQueueQuery query,
        CancellationToken ct = default);

    Task<PagedResult<LowStockAlertDto>> GetLowStockAlertsAsync(
        Guid pharmacyId,
        Guid branchId,
        int threshold = 5,
        int page = 1,
        int pageSize = 20,
        CancellationToken ct = default);

    Task<PagedResult<BranchActivityFeedItemDto>> GetBranchActivityFeedAsync(
        Guid pharmacyId,
        Guid branchId,
        int page = 1,
        int pageSize = 20,
        CancellationToken ct = default);
}
