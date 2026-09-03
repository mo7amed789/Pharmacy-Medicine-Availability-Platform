using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PharmacyAvailability.Application.Availability;
using PharmacyAvailability.Contracts.Availability;

namespace PharmacyAvailability.Api.Controllers;

[ApiController]
[AllowAnonymous]
public class AvailabilityController : ControllerBase
{
    private readonly IAvailabilityService _availabilityService;

    public AvailabilityController(IAvailabilityService availabilityService)
    {
        _availabilityService = availabilityService;
    }

    /// <summary>
    /// Public search endpoint to locate medicines across nearby active pharmacy branches.
    /// </summary>
    [HttpGet("api/v1/availability")]
    public async Task<ActionResult<AvailabilitySearchResult>> SearchAvailability(
        [FromQuery] AvailabilitySearchRequest request,
        CancellationToken ct)
    {
        var result = await _availabilityService.SearchAvailabilityAsync(request, ct);
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

        var result = await _availabilityService.SearchAvailabilityAsync(specificRequest, ct);
        return Ok(result);
    }
}
