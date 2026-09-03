using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PharmacyAvailability.Api.Authorization;
using PharmacyAvailability.Application.Pharmacies;
using PharmacyAvailability.Contracts.Pharmacies;

namespace PharmacyAvailability.Api.Controllers;

[ApiController]
[Route("api/v1/pharmacies/{pharmacyId:guid}")]
[Authorize]
public class PharmacyManagementController : ControllerBase
{
    private readonly IPharmacyService _pharmacyService;
    private readonly IAuthorizationService _authorizationService;

    public PharmacyManagementController(
        IPharmacyService pharmacyService,
        IAuthorizationService authorizationService)
    {
        _pharmacyService = pharmacyService;
        _authorizationService = authorizationService;
    }

    private async Task<bool> AuthorizePharmacyAccessAsync(Guid pharmacyId)
    {
        var authResult = await _authorizationService.AuthorizeAsync(
            User,
            pharmacyId,
            new PharmacyOwnershipRequirement());

        return authResult.Succeeded;
    }

    private bool IsPharmacyAdminOrPlatformAdmin()
    {
        return User.IsInRole("PharmacyAdmin") || User.IsInRole("PlatformAdmin");
    }

    [HttpGet("profile")]
    public async Task<ActionResult<PharmacyDetailDto>> GetProfile(
        [FromRoute] Guid pharmacyId,
        CancellationToken ct)
    {
        if (!await AuthorizePharmacyAccessAsync(pharmacyId))
            return Forbid();

        var details = await _pharmacyService.GetPharmacyDetailsAsync(pharmacyId, activeBranchesOnly: false, ct);
        if (details == null)
            return NotFound();

        return Ok(details);
    }

    [HttpPut("profile")]
    public async Task<ActionResult<PharmacyDto>> UpdateProfile(
        [FromRoute] Guid pharmacyId,
        [FromBody] UpdatePharmacyProfileRequest request,
        CancellationToken ct)
    {
        if (!await AuthorizePharmacyAccessAsync(pharmacyId))
            return Forbid();

        if (!IsPharmacyAdminOrPlatformAdmin())
            return Forbid();

        var updated = await _pharmacyService.UpdatePharmacyProfileAsync(pharmacyId, request, ct);
        return Ok(updated);
    }

    [HttpGet("branches")]
    public async Task<ActionResult<IReadOnlyList<PharmacyBranchDto>>> GetBranches(
        [FromRoute] Guid pharmacyId,
        CancellationToken ct)
    {
        if (!await AuthorizePharmacyAccessAsync(pharmacyId))
            return Forbid();

        var branches = await _pharmacyService.GetBranchesForPharmacyAsync(pharmacyId, ct);
        return Ok(branches);
    }

    [HttpPost("branches")]
    public async Task<ActionResult<PharmacyBranchDto>> CreateBranch(
        [FromRoute] Guid pharmacyId,
        [FromBody] CreateBranchRequest request,
        CancellationToken ct)
    {
        if (!await AuthorizePharmacyAccessAsync(pharmacyId))
            return Forbid();

        if (!IsPharmacyAdminOrPlatformAdmin())
            return Forbid();

        var branch = await _pharmacyService.CreateBranchAsync(pharmacyId, request, ct);
        return CreatedAtAction(
            actionName: nameof(PharmaciesController.GetBranchById),
            controllerName: "Pharmacies",
            routeValues: new { id = branch.Id },
            value: branch);
    }

    [HttpPut("branches/{branchId:guid}")]
    public async Task<ActionResult<PharmacyBranchDto>> UpdateBranch(
        [FromRoute] Guid pharmacyId,
        [FromRoute] Guid branchId,
        [FromBody] UpdateBranchRequest request,
        CancellationToken ct)
    {
        if (!await AuthorizePharmacyAccessAsync(pharmacyId))
            return Forbid();

        if (!IsPharmacyAdminOrPlatformAdmin())
            return Forbid();

        var branch = await _pharmacyService.UpdateBranchAsync(pharmacyId, branchId, request, ct);
        return Ok(branch);
    }

    [HttpPatch("branches/{branchId:guid}/status")]
    public async Task<ActionResult<PharmacyBranchDto>> UpdateBranchStatus(
        [FromRoute] Guid pharmacyId,
        [FromRoute] Guid branchId,
        [FromBody] UpdateBranchStatusRequest request,
        CancellationToken ct)
    {
        if (!await AuthorizePharmacyAccessAsync(pharmacyId))
            return Forbid();

        // Both PharmacyAdmin and PharmacyStaff can toggle branch operational status
        var branch = await _pharmacyService.UpdateBranchStatusAsync(pharmacyId, branchId, request.IsActive, ct);
        return Ok(branch);
    }

    [HttpGet("staff")]
    public async Task<ActionResult<IReadOnlyList<PharmacyStaffDto>>> GetStaff(
        [FromRoute] Guid pharmacyId,
        CancellationToken ct)
    {
        if (!await AuthorizePharmacyAccessAsync(pharmacyId))
            return Forbid();

        if (!IsPharmacyAdminOrPlatformAdmin())
            return Forbid();

        var staff = await _pharmacyService.GetStaffAsync(pharmacyId, ct);
        return Ok(staff);
    }

    [HttpPost("staff")]
    public async Task<ActionResult<PharmacyStaffDto>> AddStaff(
        [FromRoute] Guid pharmacyId,
        [FromBody] InviteStaffRequest request,
        CancellationToken ct)
    {
        if (!await AuthorizePharmacyAccessAsync(pharmacyId))
            return Forbid();

        if (!IsPharmacyAdminOrPlatformAdmin())
            return Forbid();

        var staff = await _pharmacyService.AddStaffAsync(pharmacyId, request, ct);
        return Ok(staff);
    }
}
