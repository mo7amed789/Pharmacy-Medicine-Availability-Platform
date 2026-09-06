using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
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
    public async Task Register_Success_Returns201AndTokensWithCustomerRoleAndUnverifiedStatus()
    {
        // Arrange
        var email = $"patient_{Guid.NewGuid():N}@test.com";
        var phone = $"+2010{Random.Shared.Next(10000000, 99999999)}";
        var request = new RegisterRequest(email, "SecurePassword123!", "Ahmed Ali", phone);

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/auth/register", request);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var auth = await response.Content.ReadFromJsonAsync<AuthResponse>();

        Assert.NotNull(auth);
        Assert.False(string.IsNullOrWhiteSpace(auth.AccessToken));
        Assert.False(string.IsNullOrWhiteSpace(auth.RefreshToken));
        Assert.Equal(email.ToLowerInvariant(), auth.User.Email);
        Assert.Equal(phone, auth.User.PhoneNumber);
        Assert.Equal("Customer", auth.User.Role);
        Assert.Null(auth.User.PharmacyId);
        Assert.False(auth.User.IsEmailVerified);
        Assert.False(auth.User.IsPhoneVerified);

        // Verify verification codes were created in database
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PharmacyDbContext>();
        var codes = await db.VerificationCodes.Where(vc => vc.UserId == auth.User.Id).ToListAsync();
        Assert.Equal(2, codes.Count);
        Assert.Contains(codes, c => c.Type == VerificationType.Email && !c.IsUsed);
        Assert.Contains(codes, c => c.Type == VerificationType.Phone && !c.IsUsed);
    }

    [Fact]
    public async Task Register_MissingPhone_Returns400BadRequest()
    {
        // Arrange
        var email = $"patient_nophone_{Guid.NewGuid():N}@test.com";
        var request = new RegisterRequest(email, "SecurePassword123!", "Ahmed Ali", "");

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/auth/register", request);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("Phone number is required", content);
    }

    [Fact]
    public async Task Register_ValidationFailure_ReturnsValidationProblemDetailsWithFieldErrors()
    {
        // Arrange: Invalid email, short password, empty name, invalid phone
        var request = new RegisterRequest("invalid-email", "short", "", "abc");

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/auth/register", request);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>();
        Assert.NotNull(problem);
        Assert.Equal(400, problem.Status);
        Assert.True(problem.Errors.ContainsKey("Email"));
        Assert.True(problem.Errors.ContainsKey("Password"));
        Assert.True(problem.Errors.ContainsKey("FullName"));
        Assert.True(problem.Errors.ContainsKey("PhoneNumber"));
    }

    [Fact]
    public async Task Register_DuplicateEmail_Returns400BadRequest()
    {
        // Arrange
        var email = $"duplicate_{Guid.NewGuid():N}@test.com";
        var phone = $"+2010{Random.Shared.Next(10000000, 99999999)}";
        var request = new RegisterRequest(email, "SecurePassword123!", "User One", phone);

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
        var phone = $"+2010{Random.Shared.Next(10000000, 99999999)}";
        var request = new RegisterRequest($"weak_{Guid.NewGuid():N}@test.com", "short", "Weak User", phone);

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/auth/register", request);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("at least 8 characters", content);
    }

    [Fact]
    public async Task Verify_ValidEmailAndPhoneCodes_Succeeds()
    {
        // Arrange: Register customer
        var email = $"verify_test_{Guid.NewGuid():N}@test.com";
        var phone = $"+2010{Random.Shared.Next(10000000, 99999999)}";
        var regResp = await _client.PostAsJsonAsync("/api/v1/auth/register", new RegisterRequest(email, "SecurePassword123!", "Verify User", phone));
        var auth = await regResp.Content.ReadFromJsonAsync<AuthResponse>();

        // Retrieve the generated verification codes from DbContext
        string emailCode;
        string phoneCode;
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<PharmacyDbContext>();
            var codes = await db.VerificationCodes.Where(vc => vc.UserId == auth!.User.Id).ToListAsync();
            emailCode = codes.First(c => c.Type == VerificationType.Email).Code;
            phoneCode = codes.First(c => c.Type == VerificationType.Phone).Code;
        }

        // Set authorization bearer
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth!.AccessToken);

        // Act 1: Verify Email
        var emailVerifyResp = await client.PostAsJsonAsync("/api/v1/auth/verify", new VerifyCodeRequest("Email", emailCode));
        Assert.Equal(HttpStatusCode.OK, emailVerifyResp.StatusCode);
        var status1 = await emailVerifyResp.Content.ReadFromJsonAsync<VerificationStatusResponse>();
        Assert.NotNull(status1);
        Assert.True(status1.IsEmailVerified);
        Assert.False(status1.IsPhoneVerified);

        // Act 2: Verify Phone
        var phoneVerifyResp = await client.PostAsJsonAsync("/api/v1/auth/verify", new VerifyCodeRequest("Phone", phoneCode));
        Assert.Equal(HttpStatusCode.OK, phoneVerifyResp.StatusCode);
        var status2 = await phoneVerifyResp.Content.ReadFromJsonAsync<VerificationStatusResponse>();
        Assert.NotNull(status2);
        Assert.True(status2.IsEmailVerified);
        Assert.True(status2.IsPhoneVerified);

        // Act 3: Check status endpoint
        var statusEndpointResp = await client.GetAsync("/api/v1/auth/verify/status");
        Assert.Equal(HttpStatusCode.OK, statusEndpointResp.StatusCode);
        var finalStatus = await statusEndpointResp.Content.ReadFromJsonAsync<VerificationStatusResponse>();
        Assert.NotNull(finalStatus);
        Assert.True(finalStatus.IsEmailVerified);
        Assert.True(finalStatus.IsPhoneVerified);
    }

    [Fact]
    public async Task Verify_InvalidCode_Returns400BadRequest()
    {
        // Arrange
        var email = $"verify_wrong_{Guid.NewGuid():N}@test.com";
        var phone = $"+2010{Random.Shared.Next(10000000, 99999999)}";
        var regResp = await _client.PostAsJsonAsync("/api/v1/auth/register", new RegisterRequest(email, "SecurePassword123!", "Wrong Code User", phone));
        var auth = await regResp.Content.ReadFromJsonAsync<AuthResponse>();

        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth!.AccessToken);

        // Act
        var response = await client.PostAsJsonAsync("/api/v1/auth/verify", new VerifyCodeRequest("Email", "000000"));

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("Invalid verification code", content);
    }

    [Fact]
    public async Task Verify_ExpiredCode_Returns400BadRequest()
    {
        // Arrange
        var email = $"verify_exp_{Guid.NewGuid():N}@test.com";
        var phone = $"+2010{Random.Shared.Next(10000000, 99999999)}";
        var regResp = await _client.PostAsJsonAsync("/api/v1/auth/register", new RegisterRequest(email, "SecurePassword123!", "Expired Code User", phone));
        var auth = await regResp.Content.ReadFromJsonAsync<AuthResponse>();

        // Artificially expire the code in DbContext
        string emailCode;
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<PharmacyDbContext>();
            var code = await db.VerificationCodes.FirstAsync(vc => vc.UserId == auth!.User.Id && vc.Type == VerificationType.Email);
            emailCode = code.Code;
            // Set expiration to past via reflection/property since it has private set
            typeof(VerificationCode).GetProperty("ExpiresAt")!.SetValue(code, DateTime.UtcNow.AddMinutes(-5));
            await db.SaveChangesAsync();
        }

        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth!.AccessToken);

        // Act
        var response = await client.PostAsJsonAsync("/api/v1/auth/verify", new VerifyCodeRequest("Email", emailCode));

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("expired", content, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ResendVerification_Throttling_EnforcesCooldown()
    {
        // Arrange
        var email = $"resend_test_{Guid.NewGuid():N}@test.com";
        var phone = $"+2010{Random.Shared.Next(10000000, 99999999)}";
        var regResp = await _client.PostAsJsonAsync("/api/v1/auth/register", new RegisterRequest(email, "SecurePassword123!", "Resend User", phone));
        var auth = await regResp.Content.ReadFromJsonAsync<AuthResponse>();

        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth!.AccessToken);

        // Act: Attempt to resend immediately (registration just sent a code less than 60s ago)
        var resendResp = await client.PostAsJsonAsync("/api/v1/auth/verify/resend", new SendVerificationRequest("Email"));

        // Assert: Throttled by 60s cooldown
        Assert.Equal(HttpStatusCode.BadRequest, resendResp.StatusCode);
        var content = await resendResp.Content.ReadAsStringAsync();
        Assert.Contains("Please wait", content);
    }

    [Fact]
    public async Task Login_Success_Returns200AndValidTokens()
    {
        // Arrange
        var email = $"login_{Guid.NewGuid():N}@test.com";
        var phone = $"+2010{Random.Shared.Next(10000000, 99999999)}";
        var password = "SecurePassword123!";
        await _client.PostAsJsonAsync("/api/v1/auth/register", new RegisterRequest(email, password, "Test Login", phone));

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
        var phone = $"+2010{Random.Shared.Next(10000000, 99999999)}";
        await _client.PostAsJsonAsync("/api/v1/auth/register", new RegisterRequest(email, "CorrectPassword123!", "User", phone));

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/auth/login", new LoginRequest(email, "WrongPassword123!"));

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        var content = await response.Content.ReadAsStringAsync();
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
        var phone = $"+2010{Random.Shared.Next(10000000, 99999999)}";
        var password = "SecurePassword123!";
        var regResp = await _client.PostAsJsonAsync("/api/v1/auth/register", new RegisterRequest(email, password, "Deactivated", phone));
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
        var phone = $"+2010{Random.Shared.Next(10000000, 99999999)}";
        var password = "SecurePassword123!";
        var regResp = await _client.PostAsJsonAsync("/api/v1/auth/register", new RegisterRequest(email, password, "Deact User", phone));
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
        var phone = $"+2010{Random.Shared.Next(10000000, 99999999)}";
        var password = "SecurePassword123!";
        var regResp = await _client.PostAsJsonAsync("/api/v1/auth/register", new RegisterRequest(email, password, "Refresh User", phone));
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
        var phone = $"+2010{Random.Shared.Next(10000000, 99999999)}";
        var password = "SecurePassword123!";
        var regResp = await _client.PostAsJsonAsync("/api/v1/auth/register", new RegisterRequest(email, password, "Reuse User", phone));
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
        var phone = $"+2010{Random.Shared.Next(10000000, 99999999)}";
        var password = "SecurePassword123!";
        var regResp = await _client.PostAsJsonAsync("/api/v1/auth/register", new RegisterRequest(email, password, "Revoke User", phone));
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
        var phone = "+201099999999";
        var password = "SecurePassword123!";
        var regResp = await _client.PostAsJsonAsync("/api/v1/auth/register", new RegisterRequest(email, password, "GetMe User", phone));
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
        Assert.Equal(phone, user.PhoneNumber);
        Assert.False(user.IsEmailVerified);
        Assert.False(user.IsPhoneVerified);
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
        var phone = $"+2010{Random.Shared.Next(10000000, 99999999)}";
        var password = "SecurePassword123!";
        var regResp = await _client.PostAsJsonAsync("/api/v1/auth/register", new RegisterRequest(email, password, "Concurrent User", phone));
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
