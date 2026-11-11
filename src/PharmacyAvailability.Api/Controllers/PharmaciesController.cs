using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.RateLimiting;
using PharmacyAvailability.Application.Pharmacies;
using PharmacyAvailability.Contracts.Pharmacies;

namespace PharmacyAvailability.Api.Controllers;

[ApiController]
[Route("api/v1")]
[AllowAnonymous]
public class PharmaciesController : ControllerBase
{
    private readonly IPharmacyService _pharmacyService;

    public PharmaciesController(IPharmacyService pharmacyService)
    {
        _pharmacyService = pharmacyService;
    }

    [HttpPost("pharmacies/register")]
    [EnableRateLimiting("PublicWriteRateLimit")]
    public async Task<ActionResult<PharmacyDto>> RegisterPharmacy(
        [FromBody] RegisterPharmacyRequest request,
        CancellationToken ct)
    {
        var pharmacy = await _pharmacyService.RegisterPharmacyAsync(request, ct);
        return CreatedAtAction(
            actionName: nameof(GetPharmacyById),
            routeValues: new { id = pharmacy.Id },
            value: pharmacy);
    }

    [HttpGet("pharmacies")]
    public async Task<ActionResult<IReadOnlyList<PharmacyDto>>> GetActivePharmacies(
        [FromQuery] string? city,
        CancellationToken ct)
    {
        var pharmacies = await _pharmacyService.GetActivePharmaciesAsync(city, ct);
        return Ok(pharmacies);
    }

    [HttpGet("pharmacies/{id:guid}")]
    public async Task<ActionResult<PharmacyDetailDto>> GetPharmacyById(
        [FromRoute] Guid id,
        CancellationToken ct)
    {
        var details = await _pharmacyService.GetPharmacyDetailsAsync(id, activeBranchesOnly: true, ct);
        if (details == null)
        {
            return NotFound(new ProblemDetails
            {
                Status = StatusCodes.Status404NotFound,
                Title = "Pharmacy Not Found",
                Detail = $"No active approved pharmacy found with ID '{id}'.",
                Instance = HttpContext.Request.Path
            });
        }

        return Ok(details);
    }

    [HttpGet("branches")]
    public async Task<ActionResult<IReadOnlyList<PharmacyBranchDto>>> GetActiveBranches(
        [FromQuery] string? city,
        CancellationToken ct)
    {
        var branches = await _pharmacyService.GetActiveBranchesAsync(city, ct);
        return Ok(branches);
    }

    [HttpGet("branches/{id:guid}")]
    public async Task<ActionResult<PharmacyBranchDto>> GetBranchById(
        [FromRoute] Guid id,
        CancellationToken ct)
    {
        var branch = await _pharmacyService.GetBranchByIdAsync(id, ct);
        if (branch == null)
        {
            return NotFound(new ProblemDetails
            {
                Status = StatusCodes.Status404NotFound,
                Title = "Branch Not Found",
                Detail = $"No active branch found with ID '{id}'.",
                Instance = HttpContext.Request.Path
            });
        }

        return Ok(branch);
    }
}
