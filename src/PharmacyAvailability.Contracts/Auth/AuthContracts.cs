namespace PharmacyAvailability.Contracts.Auth;

public record RegisterRequest(
    string Email,
    string Password,
    string FullName,
    string PhoneNumber);

public record LoginRequest(
    string Email,
    string Password);

public record RefreshTokenRequest(
    string RefreshToken);

public record RevokeTokenRequest(
    string RefreshToken);

public record UserDto(
    Guid Id,
    string Email,
    string FullName,
    string PhoneNumber,
    string Role,
    Guid? PharmacyId,
    bool IsEmailVerified,
    bool IsPhoneVerified);

public record AuthResponse(
    string AccessToken,
    string RefreshToken,
    DateTime ExpiresAt,
    UserDto User);

public record VerifyCodeRequest(
    string Type,
    string Code);

public record SendVerificationRequest(
    string Type);

public record VerificationStatusResponse(
    bool IsEmailVerified,
    bool IsPhoneVerified);
