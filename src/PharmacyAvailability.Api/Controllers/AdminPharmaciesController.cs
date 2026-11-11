using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PharmacyAvailability.Api.Filters;
using PharmacyAvailability.Application.Pharmacies;
using PharmacyAvailability.Contracts.Pharmacies;
using PharmacyAvailability.Domain.Pharmacies;

namespace PharmacyAvailability.Api.Controllers;

[ApiController]
[Route("api/v1/admin/pharmacies")]
[Authorize(Policy = "PlatformAdminOnly")]
[FeatureGate("EnableManagementApis")]
public class AdminPharmaciesController : ControllerBase
{
    private readonly IPharmacyService _pharmacyService;

    public AdminPharmaciesController(IPharmacyService pharmacyService)
    {
        _pharmacyService = pharmacyService;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<PharmacyDto>>> GetAllPharmacies(
        [FromQuery] PharmacyStatus? status,
        CancellationToken ct)
    {
        var pharmacies = await _pharmacyService.GetAllPharmaciesAsync(status, ct);
        return Ok(pharmacies);
    }

    [HttpPost("{id:guid}/approve")]
    public async Task<ActionResult<PharmacyDto>> ApprovePharmacy(
        [FromRoute] Guid id,
        CancellationToken ct)
    {
        var pharmacy = await _pharmacyService.ApprovePharmacyAsync(id, ct);
        return Ok(pharmacy);
    }

    [HttpPost("{id:guid}/suspend")]
    public async Task<ActionResult<PharmacyDto>> SuspendPharmacy(
        [FromRoute] Guid id,
        CancellationToken ct)
    {
        var pharmacy = await _pharmacyService.SuspendPharmacyAsync(id, ct);
        return Ok(pharmacy);
    }

    [HttpPost("{id:guid}/reactivate")]
    public async Task<ActionResult<PharmacyDto>> ReactivatePharmacy(
        [FromRoute] Guid id,
        CancellationToken ct)
    {
        var pharmacy = await _pharmacyService.ReactivatePharmacyAsync(id, ct);
        return Ok(pharmacy);
    }

    [HttpPost("{id:guid}/deactivate")]
    public async Task<ActionResult<PharmacyDto>> DeactivatePharmacy(
        [FromRoute] Guid id,
        CancellationToken ct)
    {
        var pharmacy = await _pharmacyService.DeactivatePharmacyAsync(id, ct);
        return Ok(pharmacy);
    }
}
