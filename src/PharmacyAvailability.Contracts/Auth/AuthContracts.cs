namespace PharmacyAvailability.Contracts.Auth;

public record RegisterRequest(
    string Email,
    string Password,
    string FullName,
    string? PhoneNumber);

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
    string? PhoneNumber,
    string Role,
    Guid? PharmacyId);

public record AuthResponse(
    string AccessToken,
    string RefreshToken,
    DateTime ExpiresAt,
    UserDto User);
