using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using PharmacyAvailability.Contracts.Auth;
using PharmacyAvailability.Domain.Identity;
using PharmacyAvailability.Infrastructure.Persistence;
using Xunit;

namespace PharmacyAvailability.IntegrationTests;

public class AuthIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public AuthIntegrationTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Register_Success_Returns201AndTokensWithCustomerRole()
    {
        // Arrange
        var email = $"patient_{Guid.NewGuid():N}@test.com";
        var request = new RegisterRequest(email, "SecurePassword123!", "Ahmed Ali", "+201012345678");

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/auth/register", request);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var auth = await response.Content.ReadFromJsonAsync<AuthResponse>();

        Assert.NotNull(auth);
        Assert.False(string.IsNullOrWhiteSpace(auth.AccessToken));
        Assert.False(string.IsNullOrWhiteSpace(auth.RefreshToken));
        Assert.Equal(email.ToLowerInvariant(), auth.User.Email);
        Assert.Equal("Customer", auth.User.Role);
        Assert.Null(auth.User.PharmacyId);
    }

    [Fact]
    public async Task Register_DuplicateEmail_Returns400BadRequest()
    {
        // Arrange
        var email = $"duplicate_{Guid.NewGuid():N}@test.com";
        var request = new RegisterRequest(email, "SecurePassword123!", "User One", null);

        var first = await _client.PostAsJsonAsync("/api/v1/auth/register", request);
        Assert.Equal(HttpStatusCode.Created, first.StatusCode);

        // Act
        var second = await _client.PostAsJsonAsync("/api/v1/auth/register", request);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, second.StatusCode);
        var content = await second.Content.ReadAsStringAsync();
        Assert.Contains("already exists", content);
    }

    [Fact]
    public async Task Register_WeakPassword_Returns400BadRequest()
    {
        // Arrange
        var request = new RegisterRequest($"weak_{Guid.NewGuid():N}@test.com", "short", "Weak User", null);

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/auth/register", request);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("at least 8 characters", content);
    }

    [Fact]
    public async Task Login_Success_Returns200AndValidTokens()
    {
        // Arrange
        var email = $"login_{Guid.NewGuid():N}@test.com";
        var password = "SecurePassword123!";
        await _client.PostAsJsonAsync("/api/v1/auth/register", new RegisterRequest(email, password, "Test Login", null));

        // Act
        var loginResponse = await _client.PostAsJsonAsync("/api/v1/auth/login", new LoginRequest(email, password));

        // Assert
        Assert.Equal(HttpStatusCode.OK, loginResponse.StatusCode);
        var auth = await loginResponse.Content.ReadFromJsonAsync<AuthResponse>();
        Assert.NotNull(auth);
        Assert.False(string.IsNullOrWhiteSpace(auth.AccessToken));
        Assert.False(string.IsNullOrWhiteSpace(auth.RefreshToken));
    }

    [Fact]
    public async Task Login_InvalidPassword_Returns401GenericUnauthorized()
    {
        // Arrange
        var email = $"login_fail_{Guid.NewGuid():N}@test.com";
        await _client.PostAsJsonAsync("/api/v1/auth/register", new RegisterRequest(email, "CorrectPassword123!", "User", null));

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/auth/login", new LoginRequest(email, "WrongPassword123!"));

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        var content = await response.Content.ReadAsStringAsync();
        // Verifies generic message to prevent account enumeration
        Assert.Contains("Invalid email or password", content);
    }

    [Fact]
    public async Task Login_NonExistentEmail_Returns401GenericUnauthorized()
    {
        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/auth/login", new LoginRequest("does_not_exist@test.com", "SomePassword123!"));

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("Invalid email or password", content);
    }

    [Fact]
    public async Task Login_DeactivatedAccount_Returns401Unauthorized()
    {
        // Arrange: Register user, then deactivate via DbContext
        var email = $"deactivated_{Guid.NewGuid():N}@test.com";
        var password = "SecurePassword123!";
        var regResp = await _client.PostAsJsonAsync("/api/v1/auth/register", new RegisterRequest(email, password, "Deactivated", null));
        var auth = await regResp.Content.ReadFromJsonAsync<AuthResponse>();

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<PharmacyDbContext>();
            var user = await db.Users.FindAsync(auth!.User.Id);
            user!.Deactivate();
            await db.SaveChangesAsync();
        }

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/auth/login", new LoginRequest(email, password));

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task RefreshToken_DeactivatedAccount_Returns401Unauthorized()
    {
        // Arrange: Register user, obtain refresh token, then deactivate the account
        var email = $"deact_refresh_{Guid.NewGuid():N}@test.com";
        var password = "SecurePassword123!";
        var regResp = await _client.PostAsJsonAsync("/api/v1/auth/register", new RegisterRequest(email, password, "Deact User", null));
        var auth = await regResp.Content.ReadFromJsonAsync<AuthResponse>();

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<PharmacyDbContext>();
            var user = await db.Users.FindAsync(auth!.User.Id);
            user!.Deactivate();
            await db.SaveChangesAsync();
        }

        // Act: Attempt to refresh using previously issued token
        var response = await _client.PostAsJsonAsync("/api/v1/auth/refresh", new RefreshTokenRequest(auth!.RefreshToken));

        // Assert: Deactivated user refresh MUST be rejected with 401 Unauthorized
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task RefreshToken_Success_RotatesTokenAndIssuesNewPair()
    {
        // Arrange
        var email = $"refresh_{Guid.NewGuid():N}@test.com";
        var password = "SecurePassword123!";
        var regResp = await _client.PostAsJsonAsync("/api/v1/auth/register", new RegisterRequest(email, password, "Refresh User", null));
        var initialAuth = await regResp.Content.ReadFromJsonAsync<AuthResponse>();

        // Act
        var refreshResp = await _client.PostAsJsonAsync("/api/v1/auth/refresh", new RefreshTokenRequest(initialAuth!.RefreshToken));

        // Assert
        Assert.Equal(HttpStatusCode.OK, refreshResp.StatusCode);
        var newAuth = await refreshResp.Content.ReadFromJsonAsync<AuthResponse>();

        Assert.NotNull(newAuth);
        Assert.NotEqual(initialAuth.RefreshToken, newAuth.RefreshToken);
        Assert.NotEqual(initialAuth.AccessToken, newAuth.AccessToken);

        // Subsequent refresh using the NEW token succeeds
        var secondRefresh = await _client.PostAsJsonAsync("/api/v1/auth/refresh", new RefreshTokenRequest(newAuth.RefreshToken));
        Assert.Equal(HttpStatusCode.OK, secondRefresh.StatusCode);
    }

    [Fact]
    public async Task RefreshToken_ReuseDetected_RevokesAllSessions()
    {
        // Arrange
        var email = $"reuse_{Guid.NewGuid():N}@test.com";
        var password = "SecurePassword123!";
        var regResp = await _client.PostAsJsonAsync("/api/v1/auth/register", new RegisterRequest(email, password, "Reuse User", null));
        var initialAuth = await regResp.Content.ReadFromJsonAsync<AuthResponse>();

        // Rotate token once: initialAuth.RefreshToken is now revoked, newAuth.RefreshToken is active
        var refreshResp1 = await _client.PostAsJsonAsync("/api/v1/auth/refresh", new RefreshTokenRequest(initialAuth!.RefreshToken));
        var newAuth = await refreshResp1.Content.ReadFromJsonAsync<AuthResponse>();

        // Act: Attacker or replayed request tries to reuse the initial revoked token
        var replayedResp = await _client.PostAsJsonAsync("/api/v1/auth/refresh", new RefreshTokenRequest(initialAuth.RefreshToken));

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, replayedResp.StatusCode);

        // Security check: Because reuse was detected, the active token newAuth.RefreshToken must now be invalidated too!
        var invalidatedResp = await _client.PostAsJsonAsync("/api/v1/auth/refresh", new RefreshTokenRequest(newAuth!.RefreshToken));
        Assert.Equal(HttpStatusCode.Unauthorized, invalidatedResp.StatusCode);
    }

    [Fact]
    public async Task RevokeToken_Success_PreventsFurtherRefreshes()
    {
        // Arrange
        var email = $"revoke_{Guid.NewGuid():N}@test.com";
        var password = "SecurePassword123!";
        var regResp = await _client.PostAsJsonAsync("/api/v1/auth/register", new RegisterRequest(email, password, "Revoke User", null));
        var auth = await regResp.Content.ReadFromJsonAsync<AuthResponse>();

        // Act: Logout / revoke token
        var revokeResp = await _client.PostAsJsonAsync("/api/v1/auth/revoke", new RevokeTokenRequest(auth!.RefreshToken));
        Assert.Equal(HttpStatusCode.NoContent, revokeResp.StatusCode);

        // Assert: Attempting to refresh with revoked token fails
        var refreshResp = await _client.PostAsJsonAsync("/api/v1/auth/refresh", new RefreshTokenRequest(auth.RefreshToken));
        Assert.Equal(HttpStatusCode.Unauthorized, refreshResp.StatusCode);
    }

    [Fact]
    public async Task GetMe_Authenticated_ReturnsUserProfile()
    {
        // Arrange
        var email = $"getme_{Guid.NewGuid():N}@test.com";
        var password = "SecurePassword123!";
        var regResp = await _client.PostAsJsonAsync("/api/v1/auth/register", new RegisterRequest(email, password, "GetMe User", "+201099999999"));
        var auth = await regResp.Content.ReadFromJsonAsync<AuthResponse>();

        var request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/auth/me");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", auth!.AccessToken);

        // Act
        var response = await _client.SendAsync(request);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var user = await response.Content.ReadFromJsonAsync<UserDto>();
        Assert.NotNull(user);
        Assert.Equal(email.ToLowerInvariant(), user.Email);
        Assert.Equal("GetMe User", user.FullName);
    }

    [Fact]
    public async Task GetMe_Unauthenticated_Returns401Unauthorized()
    {
        // Act
        var response = await _client.GetAsync("/api/v1/auth/me");

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task ConcurrentRefreshAttempts_PreventDoubleRotation()
    {
        // Arrange
        var email = $"concurrent_refresh_{Guid.NewGuid():N}@test.com";
        var password = "SecurePassword123!";
        var regResp = await _client.PostAsJsonAsync("/api/v1/auth/register", new RegisterRequest(email, password, "Concurrent User", null));
        var initialAuth = await regResp.Content.ReadFromJsonAsync<AuthResponse>();

        // Act: Fire two simultaneous refresh requests using the exact same refresh token
        var task1 = _client.PostAsJsonAsync("/api/v1/auth/refresh", new RefreshTokenRequest(initialAuth!.RefreshToken));
        var task2 = _client.PostAsJsonAsync("/api/v1/auth/refresh", new RefreshTokenRequest(initialAuth.RefreshToken));

        var responses = await Task.WhenAll(task1, task2);

        // Assert: At most ONE request can succeed (200 OK), the other MUST be rejected (401 Unauthorized or 409 Conflict)
        var successCount = responses.Count(r => r.StatusCode == HttpStatusCode.OK);
        var failureCount = responses.Count(r => r.StatusCode == HttpStatusCode.Unauthorized || r.StatusCode == HttpStatusCode.Conflict);

        Assert.True(successCount <= 1, "Concurrency violation: Both concurrent requests rotated the same refresh token!");
        Assert.True(failureCount >= 1, "At least one concurrent request must be rejected!");
    }
}
