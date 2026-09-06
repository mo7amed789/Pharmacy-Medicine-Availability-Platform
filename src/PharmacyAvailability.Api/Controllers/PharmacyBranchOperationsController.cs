using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PharmacyAvailability.Application.Operations;
using PharmacyAvailability.Contracts.Catalog;
using PharmacyAvailability.Contracts.Operations;
using PharmacyAvailability.Domain.Identity;

namespace PharmacyAvailability.Api.Controllers;

[ApiController]
[Route("api/v1/pharmacies/{pharmacyId:guid}/branches/{branchId:guid}/operations")]
[Authorize(Roles = "PharmacyStaff,PharmacyAdmin,PlatformAdmin")]
public class PharmacyBranchOperationsController : ControllerBase
{
    private readonly IPharmacyOperationsService _operationsService;

    public PharmacyBranchOperationsController(IPharmacyOperationsService operationsService)
    {
        _operationsService = operationsService;
    }

    private bool IsAuthorizedForPharmacy(Guid pharmacyId)
    {
        if (User.IsInRole(UserRole.PlatformAdmin.ToString()))
            return true;

        var claim = User.FindFirst("pharmacy_id")?.Value;
        return Guid.TryParse(claim, out var userPharmacyId) && userPharmacyId == pharmacyId;
    }

    /// <summary>
    /// Real-time operational dashboard for branch staff (active holds, expiring soon, today's pickups, low stock count).
    /// </summary>
    [HttpGet("dashboard")]
    public async Task<ActionResult<BranchDashboardDto>> GetDashboard(
        [FromRoute] Guid pharmacyId,
        [FromRoute] Guid branchId,
        CancellationToken ct)
    {
        if (!IsAuthorizedForPharmacy(pharmacyId))
            return Forbid();

        var result = await _operationsService.GetBranchDashboardAsync(pharmacyId, branchId, ct);
        return Ok(result);
    }

    /// <summary>
    /// Staff fulfillment reservation queue with customer search, urgency indicator, and total amount due.
    /// </summary>
    [HttpGet("reservations/queue")]
    public async Task<ActionResult<PagedResult<StaffReservationQueueItemDto>>> GetReservationQueue(
        [FromRoute] Guid pharmacyId,
        [FromRoute] Guid branchId,
        [FromQuery] StaffReservationQueueQuery query,
        CancellationToken ct)
    {
        if (!IsAuthorizedForPharmacy(pharmacyId))
            return Forbid();

        var result = await _operationsService.GetStaffReservationQueueAsync(pharmacyId, branchId, query, ct);
        return Ok(result);
    }

    /// <summary>
    /// Inventory health monitoring with low-stock alerts at or below the specified threshold.
    /// </summary>
    [HttpGet("inventory/low-stock")]
    public async Task<ActionResult<PagedResult<LowStockAlertDto>>> GetLowStockAlerts(
        [FromRoute] Guid pharmacyId,
        [FromRoute] Guid branchId,
        [FromQuery] [Range(0, 100000)] int threshold = 5,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        if (!IsAuthorizedForPharmacy(pharmacyId))
            return Forbid();

        var result = await _operationsService.GetLowStockAlertsAsync(pharmacyId, branchId, threshold, page, pageSize, ct);
        return Ok(result);
    }

    /// <summary>
    /// Chronological branch activity feed showing inventory adjustments and reservation transaction events.
    /// </summary>
    [HttpGet("activity")]
    public async Task<ActionResult<PagedResult<BranchActivityFeedItemDto>>> GetBranchActivity(
        [FromRoute] Guid pharmacyId,
        [FromRoute] Guid branchId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        if (!IsAuthorizedForPharmacy(pharmacyId))
            return Forbid();

        var result = await _operationsService.GetBranchActivityFeedAsync(pharmacyId, branchId, page, pageSize, ct);
        return Ok(result);
    }
}
