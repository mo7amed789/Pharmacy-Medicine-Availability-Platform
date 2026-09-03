using System.Net;
using System.Net.Http.Headers;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using PharmacyAvailability.Application.Common.Interfaces;
using PharmacyAvailability.Domain.Identity;
using PharmacyAvailability.Domain.Pharmacies;
using PharmacyAvailability.Infrastructure.Persistence;
using Xunit;

namespace PharmacyAvailability.IntegrationTests;

public class MultiTenancyAndRoleAuthorizationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public MultiTenancyAndRoleAuthorizationTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    private async Task<(Pharmacy PharmacyA, Pharmacy PharmacyB)> CreatePharmaciesAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PharmacyDbContext>();

        var pharmacyA = new Pharmacy($"Pharmacy A {Guid.NewGuid():N}", $"LIC-{Guid.NewGuid():N}", "123 Main St", "Cairo", new Coordinates(30.0444, 31.2357), "+201011111111");
        var pharmacyB = new Pharmacy($"Pharmacy B {Guid.NewGuid():N}", $"LIC-{Guid.NewGuid():N}", "456 Oak St", "Cairo", new Coordinates(30.0500, 31.2400), "+201022222222");

        db.Pharmacies.AddRange(pharmacyA, pharmacyB);
        await db.SaveChangesAsync();

        return (pharmacyA, pharmacyB);
    }

    private string CreateTokenForUser(User user)
    {
        using var scope = _factory.Services.CreateScope();
        var tokenService = scope.ServiceProvider.GetRequiredService<ITokenService>();
        var (token, _) = tokenService.GenerateAccessToken(user);
        return token;
    }

    [Fact]
    public async Task Customer_AttemptingPharmacyAccessCheck_Returns403Forbidden()
    {
        // Arrange
        var (pharmacyA, _) = await CreatePharmaciesAsync();
        var customer = new User("cust@test.com", "hash", "Customer", UserRole.Customer);
        var token = CreateTokenForUser(customer);

        var request = new HttpRequestMessage(HttpMethod.Get, $"/api/v1/pharmacies/{pharmacyA.Id}/access-check");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await _client.SendAsync(request);

        // Assert
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task PharmacyStaff_AccessingOwnPharmacy_Returns200Ok()
    {
        // Arrange
        var (pharmacyA, _) = await CreatePharmaciesAsync();
        var staffA = new User("staffA@test.com", "hash", "Staff A", UserRole.PharmacyStaff, "+2010", pharmacyA.Id);
        var token = CreateTokenForUser(staffA);

        var request = new HttpRequestMessage(HttpMethod.Get, $"/api/v1/pharmacies/{pharmacyA.Id}/access-check");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await _client.SendAsync(request);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task PharmacyStaff_AccessingOtherPharmacy_Returns403Forbidden()
    {
        // Arrange: Staff belongs to Pharmacy A, tries to access Pharmacy B
        var (pharmacyA, pharmacyB) = await CreatePharmaciesAsync();
        var staffA = new User("staffA@test.com", "hash", "Staff A", UserRole.PharmacyStaff, "+2010", pharmacyA.Id);
        var token = CreateTokenForUser(staffA);

        var request = new HttpRequestMessage(HttpMethod.Get, $"/api/v1/pharmacies/{pharmacyB.Id}/access-check");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await _client.SendAsync(request);

        // Assert: Multi-tenant boundary MUST reject cross-pharmacy IDOR attempt!
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task PharmacyAdmin_AccessingOtherPharmacy_Returns403Forbidden()
    {
        // Arrange: Admin of Pharmacy A tries to administer Pharmacy B
        var (pharmacyA, pharmacyB) = await CreatePharmaciesAsync();
        var adminA = new User("adminA@test.com", "hash", "Admin A", UserRole.PharmacyAdmin, "+2010", pharmacyA.Id);
        var token = CreateTokenForUser(adminA);

        var request = new HttpRequestMessage(HttpMethod.Get, $"/api/v1/pharmacies/{pharmacyB.Id}/access-check");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await _client.SendAsync(request);

        // Assert
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task PlatformAdmin_CanAccessAnyPharmacy_Returns200Ok()
    {
        // Arrange: Platform Admin with global oversight
        var (pharmacyA, pharmacyB) = await CreatePharmaciesAsync();
        var superAdmin = new User("super@platform.com", "hash", "Super Admin", UserRole.PlatformAdmin);
        var token = CreateTokenForUser(superAdmin);

        var requestA = new HttpRequestMessage(HttpMethod.Get, $"/api/v1/pharmacies/{pharmacyA.Id}/access-check");
        requestA.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var responseA = await _client.SendAsync(requestA);

        var requestB = new HttpRequestMessage(HttpMethod.Get, $"/api/v1/pharmacies/{pharmacyB.Id}/access-check");
        requestB.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var responseB = await _client.SendAsync(requestB);

        // Assert
        Assert.Equal(HttpStatusCode.OK, responseA.StatusCode);
        Assert.Equal(HttpStatusCode.OK, responseB.StatusCode);
    }

    [Fact]
    public async Task PlatformAdminCheck_PharmacyStaff_Returns403Forbidden()
    {
        // Arrange
        var (pharmacyA, _) = await CreatePharmaciesAsync();
        var staffA = new User("staffA@test.com", "hash", "Staff A", UserRole.PharmacyStaff, "+2010", pharmacyA.Id);
        var token = CreateTokenForUser(staffA);

        var request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/admin/platform-check");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await _client.SendAsync(request);

        // Assert
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task PlatformAdminCheck_PlatformAdmin_Returns200Ok()
    {
        // Arrange
        var superAdmin = new User("super@platform.com", "hash", "Super Admin", UserRole.PlatformAdmin);
        var token = CreateTokenForUser(superAdmin);

        var request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/admin/platform-check");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await _client.SendAsync(request);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task UnauthenticatedUser_AccessingProtectedEndpoint_Returns401Unauthorized()
    {
        // Act
        var response = await _client.GetAsync("/api/v1/admin/platform-check");

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
