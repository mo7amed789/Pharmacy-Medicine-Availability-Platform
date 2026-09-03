using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PharmacyAvailability.Api.Authorization;

namespace PharmacyAvailability.Api.Controllers;

[ApiController]
[Route("api/v1")]
public class PharmacyAccessTestController : ControllerBase
{
    private readonly IAuthorizationService _authorizationService;

    public PharmacyAccessTestController(IAuthorizationService authorizationService)
    {
        _authorizationService = authorizationService;
    }

    [HttpGet("pharmacies/{pharmacyId:guid}/access-check")]
    [Authorize]
    public async Task<IActionResult> CheckPharmacyAccess([FromRoute] Guid pharmacyId)
    {
        var authResult = await _authorizationService.AuthorizeAsync(
            User,
            pharmacyId,
            new PharmacyOwnershipRequirement());

        if (!authResult.Succeeded)
        {
            return Forbid();
        }

        return Ok(new
        {
            message = "Authorized access to pharmacy resource.",
            pharmacyId
        });
    }

    [HttpGet("admin/platform-check")]
    [Authorize(Policy = "PlatformAdminOnly")]
    public IActionResult CheckPlatformAdminAccess()
    {
        return Ok(new { message = "Authorized as Platform Administrator." });
    }
}
