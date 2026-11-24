using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using PharmacyAvailability.Application.Availability;
using PharmacyAvailability.Application.Credits;
using PharmacyAvailability.Contracts.Availability;

namespace PharmacyAvailability.Api.Controllers;

[ApiController]
[AllowAnonymous]
public class AvailabilityController : ControllerBase
{
    private readonly IAvailabilityService _availabilityService;
    private readonly ICreditService _credits;

    public AvailabilityController(IAvailabilityService availabilityService, ICreditService credits)
    {
        _availabilityService = availabilityService;
        _credits = credits;
    }

    private Guid? CurrentUserId()
    {
        var raw = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? User.FindFirst("sub")?.Value;
        return Guid.TryParse(raw, out var id) ? id : null;
    }

    private async Task<AvailabilitySearchResult> RunMeteredAsync(AvailabilitySearchRequest request, CancellationToken ct)
    {
        var outcome = await _credits.SearchMeteredAsync(
            CurrentUserId(),
            token => _availabilityService.SearchAvailabilityAsync(request, token),
            ct);
        Response.Headers["X-Points-Charged"] = outcome.PointsCharged.ToString();
        return outcome.Result;
    }

    /// <summary>
    /// Public search endpoint to locate medicines across nearby active pharmacy branches.
    /// </summary>
    [HttpGet("api/v1/availability")]
    public async Task<ActionResult<AvailabilitySearchResult>> SearchAvailability(
        [FromQuery] AvailabilitySearchRequest request,
        CancellationToken ct)
    {
        var result = await RunMeteredAsync(request, ct);
        return Ok(result);
    }

    /// <summary>
    /// Convenience endpoint to locate a specific medicine by ID across nearby active pharmacy branches.
    /// </summary>
    [HttpGet("api/v1/medicines/{medicineId:guid}/availability")]
    public async Task<ActionResult<AvailabilitySearchResult>> SearchMedicineAvailability(
        [FromRoute] Guid medicineId,
        [FromQuery] AvailabilitySearchRequest request,
        CancellationToken ct)
    {
        var specificRequest = new AvailabilitySearchRequest
        {
            MedicineId = medicineId,
            Latitude = request.Latitude,
            Longitude = request.Longitude,
            RadiusKm = request.RadiusKm,
            SortBy = request.SortBy,
            Page = request.Page,
            PageSize = request.PageSize
        };

        var result = await RunMeteredAsync(specificRequest, ct);
        return Ok(result);
    }
}
