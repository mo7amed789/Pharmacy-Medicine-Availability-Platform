using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using PharmacyAvailability.Application.Auth;
using PharmacyAvailability.Application.Common.Interfaces;
using PharmacyAvailability.Contracts.Auth;
using PharmacyAvailability.Domain.Common.Exceptions;
using PharmacyAvailability.Domain.Identity;
using PharmacyAvailability.Infrastructure.Persistence;

namespace PharmacyAvailability.Infrastructure.Security;

public class AuthService : IAuthService
{
    private readonly PharmacyDbContext _context;
    private readonly IPasswordHashService _passwordHasher;
    private readonly ITokenService _tokenService;
    private readonly ILogger<AuthService> _logger;

    public AuthService(
        PharmacyDbContext context,
        IPasswordHashService passwordHasher,
        ITokenService tokenService,
        ILogger<AuthService> logger)
    {
        _context = context;
        _passwordHasher = passwordHasher;
        _tokenService = tokenService;
        _logger = logger;
    }

    public async Task<AuthResponse> RegisterCustomerAsync(RegisterRequest request, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(request.Email))
            throw new DomainValidationException("Email is required.");
        if (string.IsNullOrWhiteSpace(request.Password) || request.Password.Length < 8)
            throw new DomainValidationException("Password must be at least 8 characters long.");
        if (string.IsNullOrWhiteSpace(request.FullName))
            throw new DomainValidationException("Full name is required.");

        var normalizedEmail = request.Email.Trim().ToLowerInvariant();

        var existing = await _context.Users.AnyAsync(u => u.Email == normalizedEmail, ct);
        if (existing)
        {
            _logger.LogWarning("Registration failed: Email '{Email}' is already registered.", normalizedEmail);
            throw new DomainValidationException("An account with this email address already exists.");
        }

        // Dummy user to hash password
        var dummyUser = new User(normalizedEmail, "temp", request.FullName.Trim(), UserRole.Customer);
        var hashedPassword = _passwordHasher.HashPassword(dummyUser, request.Password);

        // Security requirement: Public registration ALWAYS produces a Customer role.
        var user = new User(
            normalizedEmail,
            hashedPassword,
            request.FullName.Trim(),
            UserRole.Customer,
            request.PhoneNumber?.Trim());

        var (accessToken, accessExpiry) = _tokenService.GenerateAccessToken(user);
        var (rawRefresh, refreshHash, refreshExpiry) = _tokenService.GenerateRefreshToken();

        user.AddRefreshToken(refreshHash, refreshExpiry);

        _context.Users.Add(user);
        await _context.SaveChangesAsync(ct);

        _logger.LogInformation("New customer registered successfully: {UserId} ({Email}).", user.Id, user.Email);

        return new AuthResponse(
            accessToken,
            rawRefresh,
            accessExpiry,
            MapUserToDto(user));
    }

    public async Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
            throw new UnauthorizedAccessException("Invalid email or password.");

        var normalizedEmail = request.Email.Trim().ToLowerInvariant();

        var user = await _context.Users
            .Include(u => u.RefreshTokens)
            .FirstOrDefaultAsync(u => u.Email == normalizedEmail, ct);

        if (user == null)
        {
            _logger.LogWarning("Login failed: Account not found for email '{Email}'.", normalizedEmail);
            throw new UnauthorizedAccessException("Invalid email or password.");
        }

        if (!user.IsActive)
        {
            _logger.LogWarning("Login failed: User {UserId} is deactivated.", user.Id);
            throw new UnauthorizedAccessException("Invalid email or password.");
        }

        var isPasswordValid = _passwordHasher.VerifyPassword(user, request.Password, user.PasswordHash);
        if (!isPasswordValid)
        {
            _logger.LogWarning("Login failed: Invalid credentials for user {UserId}.", user.Id);
            throw new UnauthorizedAccessException("Invalid email or password.");
        }

        user.RecordLogin();

        var (accessToken, accessExpiry) = _tokenService.GenerateAccessToken(user);
        var (rawRefresh, refreshHash, refreshExpiry) = _tokenService.GenerateRefreshToken();

        user.AddRefreshToken(refreshHash, refreshExpiry);

        await _context.SaveChangesAsync(ct);

        _logger.LogInformation("User {UserId} logged in successfully.", user.Id);

        return new AuthResponse(
            accessToken,
            rawRefresh,
            accessExpiry,
            MapUserToDto(user));
    }

    public async Task<AuthResponse> RefreshTokenAsync(RefreshTokenRequest request, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(request.RefreshToken))
            throw new UnauthorizedAccessException("Invalid or expired refresh token.");

        var tokenHash = _tokenService.HashToken(request.RefreshToken);

        var storedToken = await _context.RefreshTokens
            .FirstOrDefaultAsync(t => t.TokenHash == tokenHash, ct);

        if (storedToken == null)
        {
            _logger.LogWarning("Token refresh failed: Token hash not found.");
            throw new UnauthorizedAccessException("Invalid or expired refresh token.");
        }

        // Security check: Reuse Detection
        if (storedToken.IsRevoked)
        {
            _logger.LogWarning(
                "SECURITY ALERT: Revoked refresh token reuse attempt detected for token hash '{TokenHash}'. Revoking all active tokens for User '{UserId}'.",
                storedToken.TokenHash, storedToken.UserId);

            var userForRevocation = await _context.Users
                .Include(u => u.RefreshTokens)
                .FirstOrDefaultAsync(u => u.Id == storedToken.UserId, ct);

            userForRevocation?.RevokeAllRefreshTokens();
            await _context.SaveChangesAsync(ct);

            throw new UnauthorizedAccessException("Invalid or revoked refresh token.");
        }

        if (storedToken.IsExpired)
        {
            _logger.LogWarning("Token refresh failed: Token expired at {ExpiresAt}.", storedToken.ExpiresAt);
            throw new UnauthorizedAccessException("Invalid or expired refresh token.");
        }

        var user = await _context.Users
            .Include(u => u.RefreshTokens)
            .FirstOrDefaultAsync(u => u.Id == storedToken.UserId, ct);

        if (user == null || !user.IsActive)
        {
            _logger.LogWarning("Token refresh failed: User {UserId} is inactive or missing.", storedToken.UserId);
            throw new UnauthorizedAccessException("User account is inactive.");
        }

        // Rotate Refresh Token
        var (newRawRefresh, newRefreshHash, newRefreshExpiry) = _tokenService.GenerateRefreshToken();
        storedToken.Revoke(newRefreshHash);
        user.AddRefreshToken(newRefreshHash, newRefreshExpiry);

        var (accessToken, accessExpiry) = _tokenService.GenerateAccessToken(user);

        await _context.SaveChangesAsync(ct);

        _logger.LogInformation("Refresh token rotated successfully for user {UserId}.", user.Id);

        return new AuthResponse(
            accessToken,
            newRawRefresh,
            accessExpiry,
            MapUserToDto(user));
    }

    public async Task RevokeTokenAsync(RevokeTokenRequest request, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(request.RefreshToken))
            return;

        var tokenHash = _tokenService.HashToken(request.RefreshToken);
        var storedToken = await _context.RefreshTokens
            .FirstOrDefaultAsync(t => t.TokenHash == tokenHash, ct);

        if (storedToken != null && storedToken.IsActive)
        {
            storedToken.Revoke();
            await _context.SaveChangesAsync(ct);
            _logger.LogInformation("Refresh token revoked for user {UserId}.", storedToken.UserId);
        }
    }

    public async Task<UserDto> GetCurrentUserAsync(Guid userId, CancellationToken ct = default)
    {
        var user = await _context.Users.FindAsync(new object[] { userId }, ct);
        if (user == null || !user.IsActive)
            throw new UnauthorizedAccessException("User not found or inactive.");

        return MapUserToDto(user);
    }

    private static UserDto MapUserToDto(User user) =>
        new(user.Id, user.Email, user.FullName, user.PhoneNumber, user.Role.ToString(), user.PharmacyId);
}
