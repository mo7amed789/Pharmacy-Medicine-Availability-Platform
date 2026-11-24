using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PharmacyAvailability.Application.Credits;
using PharmacyAvailability.Contracts.Credits;

namespace PharmacyAvailability.Api.Controllers;

[ApiController]
public class CreditsController : ControllerBase
{
    private readonly ICreditService _credits;

    public CreditsController(ICreditService credits)
    {
        _credits = credits;
    }

    /// <summary>The signed-in user's own balance and account code (the code they send to be topped up).</summary>
    [HttpGet("api/v1/credits/me")]
    [Authorize]
    public async Task<ActionResult<MyCreditDto>> GetMine(CancellationToken ct)
    {
        var id = CurrentUserId();
        if (id is null) return Unauthorized();
        return Ok(await _credits.GetMyAccountAsync(id.Value, ct));
    }

    /// <summary>Operator: look up an account by the code the user sent.</summary>
    [HttpGet("api/v1/admin/credits/accounts/{code}")]
    [Authorize(Policy = "PlatformAdminOnly")]
    public async Task<ActionResult<AdminCreditAccountDto>> Lookup([FromRoute] string code, CancellationToken ct)
        => Ok(await _credits.LookupAccountAsync(code, ct));

    /// <summary>Operator: add points after confirming the transfer in the bank app. The transfer reference must be unique.</summary>
    [HttpPost("api/v1/admin/credits/topups")]
    [Authorize(Policy = "PlatformAdminOnly")]
    public async Task<ActionResult<AdminCreditAccountDto>> TopUp([FromBody] CreditTopUpRequest request, CancellationToken ct)
    {
        var id = CurrentUserId();
        if (id is null) return Unauthorized();
        return Ok(await _credits.TopUpAsync(id.Value, request, ct));
    }

    /// <summary>Operator: usage counts for a UTC date range (defaults to the last 7 days).</summary>
    [HttpGet("api/v1/admin/credits/usage")]
    [Authorize(Policy = "PlatformAdminOnly")]
    public async Task<ActionResult<UsageSummaryDto>> Usage([FromQuery] DateTime? fromUtc, [FromQuery] DateTime? toUtc, CancellationToken ct)
    {
        var to = toUtc ?? DateTime.UtcNow;
        var from = fromUtc ?? to.AddDays(-7);
        return Ok(await _credits.GetUsageSummaryAsync(DateTime.SpecifyKind(from, DateTimeKind.Utc), DateTime.SpecifyKind(to, DateTimeKind.Utc), ct));
    }

    private Guid? CurrentUserId()
    {
        var raw = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? User.FindFirst("sub")?.Value;
        return Guid.TryParse(raw, out var id) ? id : null;
    }
}
