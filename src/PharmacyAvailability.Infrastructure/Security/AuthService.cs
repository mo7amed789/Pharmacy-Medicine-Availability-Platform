using System.Security.Cryptography;
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
    private readonly IVerificationSender _verificationSender;
    private readonly IEmailExistenceValidator _emailValidator;
    private readonly ILogger<AuthService> _logger;

    public AuthService(
        PharmacyDbContext context,
        IPasswordHashService passwordHasher,
        ITokenService tokenService,
        IVerificationSender verificationSender,
        IEmailExistenceValidator emailValidator,
        ILogger<AuthService> logger)
    {
        _context = context;
        _passwordHasher = passwordHasher;
        _tokenService = tokenService;
        _verificationSender = verificationSender;
        _emailValidator = emailValidator;
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
        if (string.IsNullOrWhiteSpace(request.PhoneNumber))
            throw new DomainValidationException("Phone number is required.");

        var normalizedEmail = request.Email.Trim().ToLowerInvariant();
        var normalizedPhone = request.PhoneNumber.Trim();

        // Validate that the email domain exists in real life and is not disposable/fake
        var emailExistenceResult = await _emailValidator.ValidateEmailExistenceAsync(normalizedEmail, ct);
        if (!emailExistenceResult.IsValid)
        {
            _logger.LogWarning("Registration failed: Email '{Email}' failed existence check: {Reason}",
                normalizedEmail, emailExistenceResult.ErrorMessage);
            throw new DomainValidationException(emailExistenceResult.ErrorMessage ?? "The email domain does not exist or cannot receive mail.");
        }

        var existing = await _context.Users.AnyAsync(u => u.Email == normalizedEmail, ct);
        if (existing)
        {
            _logger.LogWarning("Registration failed: Email '{Email}' is already registered.", normalizedEmail);
            throw new DomainValidationException("An account with this email address already exists.");
        }

        // Dummy user to hash password
        var dummyUser = new User(normalizedEmail, "temp", request.FullName.Trim(), UserRole.Customer, normalizedPhone);
        var hashedPassword = _passwordHasher.HashPassword(dummyUser, request.Password);

        // Security requirement: Public registration ALWAYS produces a Customer role with unverified status.
        var user = new User(
            normalizedEmail,
            hashedPassword,
            request.FullName.Trim(),
            UserRole.Customer,
            normalizedPhone);

        var (accessToken, accessExpiry) = _tokenService.GenerateAccessToken(user);
        var (rawRefresh, refreshHash, refreshExpiry) = _tokenService.GenerateRefreshToken();

        user.AddRefreshToken(refreshHash, refreshExpiry);

        _context.Users.Add(user);

        // Generate and dispatch verification codes for Email & Phone
        var emailOtp = GenerateOtpCode();
        var phoneOtp = GenerateOtpCode();
        var expiry = DateTime.UtcNow.AddMinutes(10);

        var emailCode = new VerificationCode(user.Id, emailOtp, VerificationType.Email, expiry);
        var phoneCode = new VerificationCode(user.Id, phoneOtp, VerificationType.Phone, expiry);

        _context.VerificationCodes.Add(emailCode);
        _context.VerificationCodes.Add(phoneCode);

        await _context.SaveChangesAsync(ct);

        // Dispatch codes (async)
        await _verificationSender.SendEmailCodeAsync(user.Email, emailOtp, ct);
        await _verificationSender.SendPhoneCodeAsync(user.PhoneNumber, phoneOtp, ct);

        _logger.LogInformation("New customer registered successfully: {UserId} ({Email}). Verification codes dispatched.", user.Id, user.Email);

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

        try
        {
            await _context.SaveChangesAsync(ct);
        }
        catch (DbUpdateConcurrencyException ex)
        {
            _logger.LogWarning(ex, "Concurrency conflict during refresh token rotation for user {UserId}.", user.Id);
            throw new UnauthorizedAccessException("The refresh token has already been rotated by a concurrent request.");
        }

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

    public async Task<VerificationStatusResponse> VerifyCodeAsync(Guid userId, VerifyCodeRequest request, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(request.Type))
            throw new DomainValidationException("Verification type is required ('Email' or 'Phone').");
        if (string.IsNullOrWhiteSpace(request.Code))
            throw new DomainValidationException("Verification code is required.");

        var user = await _context.Users.FindAsync(new object[] { userId }, ct);
        if (user == null || !user.IsActive)
            throw new UnauthorizedAccessException("User account not found or inactive.");

        if (!Enum.TryParse<VerificationType>(request.Type.Trim(), true, out var type))
            throw new DomainValidationException("Invalid verification type. Expected 'Email' or 'Phone'.");

        // Find active verification code for this user and type
        var code = await _context.VerificationCodes
            .Where(vc => vc.UserId == userId && vc.Type == type && !vc.IsUsed)
            .OrderByDescending(vc => vc.CreatedAt)
            .FirstOrDefaultAsync(ct);

        if (code == null)
            throw new DomainValidationException("No pending verification code found. Please request a new code.");

        if (code.IsExpired)
            throw new DomainValidationException("Verification code has expired. Please request a new code.");

        var isValid = code.Verify(request.Code);
        if (!isValid)
        {
            await _context.SaveChangesAsync(ct);
            var remainingAttempts = VerificationCode.MaxAttempts - code.AttemptCount;
            throw new DomainValidationException($"Invalid verification code. {remainingAttempts} attempts remaining.");
        }

        if (type == VerificationType.Email)
        {
            user.VerifyEmail();
            _logger.LogInformation("User {UserId} successfully verified email.", userId);
        }
        else if (type == VerificationType.Phone)
        {
            user.VerifyPhone();
            _logger.LogInformation("User {UserId} successfully verified phone number.", userId);
        }

        await _context.SaveChangesAsync(ct);

        return new VerificationStatusResponse(user.IsEmailVerified, user.IsPhoneVerified);
    }

    public async Task SendVerificationCodeAsync(Guid userId, SendVerificationRequest request, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(request.Type))
            throw new DomainValidationException("Verification type is required ('Email' or 'Phone').");

        var user = await _context.Users.FindAsync(new object[] { userId }, ct);
        if (user == null || !user.IsActive)
            throw new UnauthorizedAccessException("User account not found or inactive.");

        if (!Enum.TryParse<VerificationType>(request.Type.Trim(), true, out var type))
            throw new DomainValidationException("Invalid verification type. Expected 'Email' or 'Phone'.");

        if (type == VerificationType.Email && user.IsEmailVerified)
            throw new DomainValidationException("Email is already verified.");
        if (type == VerificationType.Phone && user.IsPhoneVerified)
            throw new DomainValidationException("Phone number is already verified.");

        // Rate limiting check: 60 seconds minimum between requests
        var lastCode = await _context.VerificationCodes
            .Where(vc => vc.UserId == userId && vc.Type == type)
            .OrderByDescending(vc => vc.CreatedAt)
            .FirstOrDefaultAsync(ct);

        if (lastCode != null && (DateTime.UtcNow - lastCode.CreatedAt) < TimeSpan.FromSeconds(60))
        {
            var waitSec = 60 - (int)(DateTime.UtcNow - lastCode.CreatedAt).TotalSeconds;
            throw new DomainValidationException($"Please wait {waitSec} seconds before requesting a new code.");
        }

        // Mark previous unused codes as used to invalidate them
        if (lastCode != null && !lastCode.IsUsed)
        {
            lastCode.MarkUsed();
        }

        var newOtp = GenerateOtpCode();
        var expiry = DateTime.UtcNow.AddMinutes(10);
        var verificationCode = new VerificationCode(userId, newOtp, type, expiry);

        _context.VerificationCodes.Add(verificationCode);
        await _context.SaveChangesAsync(ct);

        if (type == VerificationType.Email)
        {
            await _verificationSender.SendEmailCodeAsync(user.Email, newOtp, ct);
        }
        else
        {
            await _verificationSender.SendPhoneCodeAsync(user.PhoneNumber, newOtp, ct);
        }

        _logger.LogInformation("Dispatched new {Type} verification code for user {UserId}.", type, userId);
    }

    public async Task<VerificationStatusResponse> GetVerificationStatusAsync(Guid userId, CancellationToken ct = default)
    {
        var user = await _context.Users.FindAsync(new object[] { userId }, ct);
        if (user == null || !user.IsActive)
            throw new UnauthorizedAccessException("User account not found or inactive.");

        return new VerificationStatusResponse(user.IsEmailVerified, user.IsPhoneVerified);
    }

    private static string GenerateOtpCode()
    {
        return RandomNumberGenerator.GetInt32(100000, 1000000).ToString();
    }

    private static UserDto MapUserToDto(User user) =>
        new(user.Id, user.Email, user.FullName, user.PhoneNumber, user.Role.ToString(), user.PharmacyId, user.IsEmailVerified, user.IsPhoneVerified);
}
