using PharmacyAvailability.Contracts.Auth;

namespace PharmacyAvailability.Application.Auth;

public interface IAuthService
{
    Task<AuthResponse> RegisterCustomerAsync(RegisterRequest request, CancellationToken ct = default);
    Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken ct = default);
    Task<AuthResponse> RefreshTokenAsync(RefreshTokenRequest request, CancellationToken ct = default);
    Task RevokeTokenAsync(RevokeTokenRequest request, CancellationToken ct = default);
    Task<UserDto> GetCurrentUserAsync(Guid userId, CancellationToken ct = default);
}
