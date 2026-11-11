using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using PharmacyAvailability.Application.Auth;
using PharmacyAvailability.Contracts.Auth;

namespace PharmacyAvailability.Api.Controllers;

[ApiController]
[Route("api/v1/auth")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    [HttpPost("register")]
    [AllowAnonymous]
    [EnableRateLimiting("AuthRateLimit")]
    public async Task<ActionResult<AuthResponse>> Register(
        [FromBody] RegisterRequest request,
        CancellationToken ct)
    {
        var response = await _authService.RegisterCustomerAsync(request, ct);
        return CreatedAtAction(nameof(GetMe), response);
    }

    [HttpPost("login")]
    [AllowAnonymous]
    [EnableRateLimiting("AuthRateLimit")]
    public async Task<ActionResult<AuthResponse>> Login(
        [FromBody] LoginRequest request,
        CancellationToken ct)
    {
        var response = await _authService.LoginAsync(request, ct);
        return Ok(response);
    }

    [HttpPost("refresh")]
    [AllowAnonymous]
    [EnableRateLimiting("AuthRateLimit")]
    public async Task<ActionResult<AuthResponse>> Refresh(
        [FromBody] RefreshTokenRequest request,
        CancellationToken ct)
    {
        var response = await _authService.RefreshTokenAsync(request, ct);
        return Ok(response);
    }

    [HttpPost("revoke")]
    [AllowAnonymous]
    [EnableRateLimiting("AuthRateLimit")]
    public async Task<IActionResult> Revoke(
        [FromBody] RevokeTokenRequest request,
        CancellationToken ct)
    {
        await _authService.RevokeTokenAsync(request, ct);
        return NoContent();
    }

    [HttpGet("me")]
    [Authorize]
    public async Task<ActionResult<UserDto>> GetMe(CancellationToken ct)
    {
        var userId = GetCurrentUserId();
        if (!userId.HasValue)
            return Unauthorized();

        var user = await _authService.GetCurrentUserAsync(userId.Value, ct);
        return Ok(user);
    }

    [HttpPost("verify")]
    [Authorize]
    [EnableRateLimiting("AuthRateLimit")]
    public async Task<ActionResult<VerificationStatusResponse>> Verify(
        [FromBody] VerifyCodeRequest request,
        CancellationToken ct)
    {
        var userId = GetCurrentUserId();
        if (!userId.HasValue)
            return Unauthorized();

        var status = await _authService.VerifyCodeAsync(userId.Value, request, ct);
        return Ok(status);
    }

    [HttpPost("verify/resend")]
    [Authorize]
    [EnableRateLimiting("AuthRateLimit")]
    public async Task<IActionResult> ResendVerification(
        [FromBody] SendVerificationRequest request,
        CancellationToken ct)
    {
        var userId = GetCurrentUserId();
        if (!userId.HasValue)
            return Unauthorized();

        await _authService.SendVerificationCodeAsync(userId.Value, request, ct);
        return Ok(new { message = $"Verification code sent for {request.Type}." });
    }

    [HttpGet("verify/status")]
    [Authorize]
    public async Task<ActionResult<VerificationStatusResponse>> GetVerificationStatus(CancellationToken ct)
    {
        var userId = GetCurrentUserId();
        if (!userId.HasValue)
            return Unauthorized();

        var status = await _authService.GetVerificationStatusAsync(userId.Value, ct);
        return Ok(status);
    }

    private Guid? GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? User.FindFirst("sub")?.Value;

        return Guid.TryParse(userIdClaim, out var userId) ? userId : null;
    }
}
