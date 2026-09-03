using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PharmacyAvailability.Application.Reservations;
using PharmacyAvailability.Contracts.Catalog;
using PharmacyAvailability.Contracts.Reservations;
using PharmacyAvailability.Domain.Identity;

namespace PharmacyAvailability.Api.Controllers;

[ApiController]
[Route("api/v1/reservations")]
[Authorize]
public class ReservationsController : ControllerBase
{
    private readonly IReservationService _reservationService;

    public ReservationsController(IReservationService reservationService)
    {
        _reservationService = reservationService;
    }

    private Guid GetCurrentUserId()
    {
        var claim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ??
                    User.FindFirst("sub")?.Value;

        return Guid.TryParse(claim, out var id) ? id : Guid.Empty;
    }

    private UserRole GetCurrentUserRole()
    {
        var roleClaim = User.FindFirst(ClaimTypes.Role)?.Value;
        return Enum.TryParse<UserRole>(roleClaim, true, out var role)
            ? role
            : UserRole.Customer;
    }

    private Guid? GetUserPharmacyId()
    {
        var claim = User.FindFirst("pharmacy_id")?.Value;
        return Guid.TryParse(claim, out var id) ? id : null;
    }

    /// <summary>
    /// Place a temporary reservation hold for a medicine at a branch.
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<ReservationDto>> CreateReservation(
        [FromBody] CreateReservationRequest request,
        CancellationToken ct)
    {
        var customerId = GetCurrentUserId();
        var result = await _reservationService.CreateReservationAsync(request, customerId, ct);

        return CreatedAtAction(
            actionName: nameof(GetReservationById),
            routeValues: new { reservationId = result.Id },
            value: result);
    }

    /// <summary>
    /// List reservations according to tenant scope (customers see own; staff see pharmacy's; admin sees all).
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<PagedResult<ReservationDto>>> GetReservations(
        [FromQuery] GetReservationsQuery query,
        CancellationToken ct)
    {
        var userId = GetCurrentUserId();
        var role = GetCurrentUserRole();
        var pharmacyId = GetUserPharmacyId();

        var result = await _reservationService.GetReservationsAsync(query, userId, role, pharmacyId, ct);
        return Ok(result);
    }

    /// <summary>
    /// Retrieve a reservation by ID with strict ownership validation.
    /// </summary>
    [HttpGet("{reservationId:guid}")]
    public async Task<ActionResult<ReservationDto>> GetReservationById(
        [FromRoute] Guid reservationId,
        CancellationToken ct)
    {
        var userId = GetCurrentUserId();
        var role = GetCurrentUserRole();
        var pharmacyId = GetUserPharmacyId();

        var result = await _reservationService.GetReservationByIdAsync(reservationId, userId, role, pharmacyId, ct);
        if (result == null)
        {
            return NotFound(new ProblemDetails
            {
                Status = StatusCodes.Status404NotFound,
                Title = "Reservation Not Found",
                Detail = $"Reservation with ID '{reservationId}' was not found.",
                Instance = HttpContext.Request.Path
            });
        }

        return Ok(result);
    }

    /// <summary>
    /// Customer cancels an active reservation hold.
    /// </summary>
    [HttpPost("{reservationId:guid}/cancel")]
    public async Task<ActionResult<ReservationDto>> CancelReservation(
        [FromRoute] Guid reservationId,
        CancellationToken ct)
    {
        var customerId = GetCurrentUserId();
        var result = await _reservationService.CancelReservationAsync(reservationId, customerId, ct);
        return Ok(result);
    }

    /// <summary>
    /// Pharmacy staff confirms customer pickup, finalizing inventory consumption.
    /// </summary>
    [HttpPost("{reservationId:guid}/confirm")]
    [Authorize(Roles = "PharmacyStaff,PharmacyAdmin,PlatformAdmin")]
    public async Task<ActionResult<ReservationDto>> ConfirmReservation(
        [FromRoute] Guid reservationId,
        CancellationToken ct)
    {
        var staffUserId = GetCurrentUserId();
        var pharmacyId = GetUserPharmacyId();
        var isPlatformAdmin = User.IsInRole(UserRole.PlatformAdmin.ToString());

        var existing = await _reservationService.GetReservationByIdAsync(
            reservationId, staffUserId, UserRole.PlatformAdmin, null, ct);

        if (existing == null)
        {
            return NotFound(new ProblemDetails
            {
                Status = StatusCodes.Status404NotFound,
                Title = "Reservation Not Found",
                Detail = $"Reservation with ID '{reservationId}' was not found.",
                Instance = HttpContext.Request.Path
            });
        }

        if (!isPlatformAdmin && (!pharmacyId.HasValue || pharmacyId.Value != existing.PharmacyId))
            return Forbid();

        var result = await _reservationService.ConfirmReservationAsync(
            reservationId,
            staffUserId,
            pharmacyId,
            isPlatformAdmin,
            ct);

        return Ok(result);
    }
}
