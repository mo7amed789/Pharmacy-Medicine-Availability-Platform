using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Hosting;
using PharmacyAvailability.Api.Authorization;

namespace PharmacyAvailability.Api.Controllers;

[ApiController]
[Route("api/test")]
[ApiExplorerSettings(IgnoreApi = true)]
public class PharmacyAccessTestController : ControllerBase
{
    private readonly IAuthorizationService _authorizationService;
    private readonly IHostEnvironment _env;

    public PharmacyAccessTestController(IAuthorizationService authorizationService, IHostEnvironment env)
    {
        _authorizationService = authorizationService;
        _env = env;
    }

    [HttpGet("pharmacies/{pharmacyId:guid}/access-check")]
    [Authorize]
    public async Task<IActionResult> CheckPharmacyAccess([FromRoute] Guid pharmacyId)
    {
        // Strictly block in production environments
        if (!_env.IsDevelopment())
        {
            return NotFound();
        }

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
        // Strictly block in production environments
        if (!_env.IsDevelopment())
        {
            return NotFound();
        }

        return Ok(new { message = "Authorized as Platform Administrator." });
    }
}
