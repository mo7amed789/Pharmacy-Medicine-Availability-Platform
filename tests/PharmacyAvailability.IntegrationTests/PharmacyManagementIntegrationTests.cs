using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using PharmacyAvailability.Application.Common.Interfaces;
using PharmacyAvailability.Contracts.Pharmacies;
using PharmacyAvailability.Domain.Identity;
using PharmacyAvailability.Domain.Pharmacies;
using PharmacyAvailability.Infrastructure.Persistence;
using Xunit;

namespace PharmacyAvailability.IntegrationTests;

public class PharmacyManagementIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public PharmacyManagementIntegrationTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    private string CreateToken(UserRole role, Guid? pharmacyId = null)
    {
        using var scope = _factory.Services.CreateScope();
        var tokenService = scope.ServiceProvider.GetRequiredService<ITokenService>();
        var user = new User($"user_{Guid.NewGuid():N}@test.com", "hash", "Test User", role, "+2010", pharmacyId);
        var (token, _) = tokenService.GenerateAccessToken(user);
        return token;
    }

    private async Task<(Pharmacy PharmacyA, Pharmacy PharmacyB)> SeedTwoPharmaciesAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PharmacyDbContext>();

        var pA = new Pharmacy($"Pharma A {Guid.NewGuid():N}", $"LIC_A_{Guid.NewGuid():N}", "123 St A", "Cairo", new Coordinates(30.0, 31.0), "+20101");
        var pB = new Pharmacy($"Pharma B {Guid.NewGuid():N}", $"LIC_B_{Guid.NewGuid():N}", "456 St B", "Giza", new Coordinates(30.1, 31.1), "+20102");

        pA.AddBranch("Branch A1", "Addr A1", "Cairo", new Coordinates(30.0, 31.0), "+20101");
        pB.AddBranch("Branch B1", "Addr B1", "Giza", new Coordinates(30.1, 31.1), "+20102");

        db.Pharmacies.AddRange(pA, pB);
        await db.SaveChangesAsync();

        return (pA, pB);
    }

    [Fact]
    public async Task RegisterPharmacy_PublicRegistration_CreatesPendingPharmacyAndAdmin()
    {
        // Arrange
        var license = $"LIC_{Guid.NewGuid():N}";
        var email = $"admin_{Guid.NewGuid():N}@pharma.com";
        var request = new RegisterPharmacyRequest(
            Name: "El Borg Pharmacy",
            LicenseNumber: license,
            Address: "50 Nile Corniche",
            City: "Maadi",
            Latitude: 29.9602,
            Longitude: 31.2569,
            Phone: "+20225190000",
            Email: "contact@elborg.com",
            OpeningHours: "24/7",
            AdminFullName: "Dr. Ahmed Hassan",
            AdminEmail: email,
            AdminPassword: "SecurePassword123!",
            AdminPhone: "+201099998888");

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/pharmacies/register", request);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var pharmacy = await response.Content.ReadFromJsonAsync<PharmacyDto>();
        Assert.NotNull(pharmacy);
        Assert.Equal("El Borg Pharmacy", pharmacy.Name);
        Assert.Equal(license.ToUpperInvariant(), pharmacy.LicenseNumber);
        Assert.Equal("Pending", pharmacy.Status); // Approval workflow requires PlatformAdmin approval
        Assert.Equal(1, pharmacy.BranchesCount);

        // Verify admin user was created in database with PharmacyAdmin role
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PharmacyDbContext>();
        var adminUser = db.Users.FirstOrDefault(u => u.Email == email);
        Assert.NotNull(adminUser);
        Assert.Equal(UserRole.PharmacyAdmin, adminUser.Role);
        Assert.Equal(pharmacy.Id, adminUser.PharmacyId);
    }

    [Fact]
    public async Task PlatformAdmin_ApprovalWorkflow_TransitionsPendingToActive()
    {
        // Arrange: Register pharmacy (status = Pending)
        var regReq = new RegisterPharmacyRequest(
            Name: "Al Shifa Pharmacy",
            LicenseNumber: $"LIC_{Guid.NewGuid():N}",
            Address: "10 Tahrir St",
            City: "Cairo",
            Latitude: 30.0444,
            Longitude: 31.2357,
            Phone: "010111",
            Email: null,
            OpeningHours: null,
            AdminFullName: "Dr. Ali",
            AdminEmail: $"ali_{Guid.NewGuid():N}@test.com",
            AdminPassword: "Password123!");

        var regResp = await _client.PostAsJsonAsync("/api/v1/pharmacies/register", regReq);
        var created = await regResp.Content.ReadFromJsonAsync<PharmacyDto>();

        var adminToken = CreateToken(UserRole.PlatformAdmin);

        // Act: Approve pharmacy
        var approveReq = new HttpRequestMessage(HttpMethod.Post, $"/api/v1/admin/pharmacies/{created!.Id}/approve");
        approveReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);
        var approveResp = await _client.SendAsync(approveReq);

        // Assert
        Assert.Equal(HttpStatusCode.OK, approveResp.StatusCode);
        var approved = await approveResp.Content.ReadFromJsonAsync<PharmacyDto>();
        Assert.NotNull(approved);
        Assert.Equal("Active", approved.Status);

        // Act 2: Suspend pharmacy
        var suspendReq = new HttpRequestMessage(HttpMethod.Post, $"/api/v1/admin/pharmacies/{created.Id}/suspend");
        suspendReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);
        var suspendResp = await _client.SendAsync(suspendReq);

        Assert.Equal(HttpStatusCode.OK, suspendResp.StatusCode);
        var suspended = await suspendResp.Content.ReadFromJsonAsync<PharmacyDto>();
        Assert.Equal("Suspended", suspended!.Status);

        // Act 3: Reactivate pharmacy
        var reactivateReq = new HttpRequestMessage(HttpMethod.Post, $"/api/v1/admin/pharmacies/{created.Id}/reactivate");
        reactivateReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);
        var reactivateResp = await _client.SendAsync(reactivateReq);

        Assert.Equal(HttpStatusCode.OK, reactivateResp.StatusCode);
        var reactivated = await reactivateResp.Content.ReadFromJsonAsync<PharmacyDto>();
        Assert.Equal("Active", reactivated!.Status);
    }

    [Fact]
    public async Task Customer_AttemptingApproval_Returns403Forbidden()
    {
        // Arrange
        var customerToken = CreateToken(UserRole.Customer);
        var request = new HttpRequestMessage(HttpMethod.Post, $"/api/v1/admin/pharmacies/{Guid.NewGuid()}/approve");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", customerToken);

        // Act
        var response = await _client.SendAsync(request);

        // Assert
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task PublicDirectory_ExcludesPendingAndSuspendedPharmacies()
    {
        // Arrange: Register a pharmacy (Pending)
        var regReq = new RegisterPharmacyRequest(
            Name: $"HiddenPharma_{Guid.NewGuid():N}",
            LicenseNumber: $"LIC_{Guid.NewGuid():N}",
            Address: "Hidden Addr",
            City: "Alexandria",
            Latitude: 31.2001,
            Longitude: 29.9187,
            Phone: "0100",
            Email: null,
            OpeningHours: null,
            AdminFullName: "Admin",
            AdminEmail: $"admin_{Guid.NewGuid():N}@test.com",
            AdminPassword: "Password123!");

        var regResp = await _client.PostAsJsonAsync("/api/v1/pharmacies/register", regReq);
        var created = await regResp.Content.ReadFromJsonAsync<PharmacyDto>();

        // Act 1: Public listing must NOT show pending pharmacy
        var publicResp1 = await _client.GetAsync("/api/v1/pharmacies");
        var list1 = await publicResp1.Content.ReadFromJsonAsync<List<PharmacyDto>>();
        Assert.DoesNotContain(list1!, p => p.Id == created!.Id);

        // Act 2: Approve pharmacy via PlatformAdmin
        var adminToken = CreateToken(UserRole.PlatformAdmin);
        var approveReq = new HttpRequestMessage(HttpMethod.Post, $"/api/v1/admin/pharmacies/{created!.Id}/approve");
        approveReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);
        await _client.SendAsync(approveReq);

        // Act 3: Public listing NOW includes approved pharmacy
        var publicResp2 = await _client.GetAsync("/api/v1/pharmacies");
        var list2 = await publicResp2.Content.ReadFromJsonAsync<List<PharmacyDto>>();
        Assert.Contains(list2!, p => p.Id == created.Id);

        // Act 4: Suspend pharmacy
        var suspendReq = new HttpRequestMessage(HttpMethod.Post, $"/api/v1/admin/pharmacies/{created.Id}/suspend");
        suspendReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);
        await _client.SendAsync(suspendReq);

        // Act 5: Public listing excludes suspended pharmacy
        var publicResp3 = await _client.GetAsync("/api/v1/pharmacies");
        var list3 = await publicResp3.Content.ReadFromJsonAsync<List<PharmacyDto>>();
        Assert.DoesNotContain(list3!, p => p.Id == created.Id);
    }

    [Fact]
    public async Task MultiTenancy_PharmacyAdmin_ManagingOwnBranches_Succeeds()
    {
        // Arrange
        var (pharmacyA, _) = await SeedTwoPharmaciesAsync();
        var adminTokenA = CreateToken(UserRole.PharmacyAdmin, pharmacyA.Id);

        var createBranchReq = new CreateBranchRequest(
            Name: "Maadi Degla Branch",
            Address: "Road 9, Maadi",
            City: "Cairo",
            Latitude: 29.9592,
            Longitude: 31.2612,
            Phone: "+201099887766",
            OpeningHours: "24/7",
            BranchCode: "MD-01");

        var httpRequest = new HttpRequestMessage(HttpMethod.Post, $"/api/v1/pharmacies/{pharmacyA.Id}/branches")
        {
            Content = JsonContent.Create(createBranchReq)
        };
        httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", adminTokenA);

        // Act
        var response = await _client.SendAsync(httpRequest);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var branch = await response.Content.ReadFromJsonAsync<PharmacyBranchDto>();
        Assert.NotNull(branch);
        Assert.Equal("Maadi Degla Branch", branch.Name);
        Assert.Equal(pharmacyA.Id, branch.PharmacyId);
    }

    [Fact]
    public async Task MultiTenancy_PharmacyAdmin_AccessingOtherPharmacy_Returns403Forbidden()
    {
        // Arrange: Admin belongs to Pharmacy A, attempts to operate on Pharmacy B
        var (pharmacyA, pharmacyB) = await SeedTwoPharmaciesAsync();
        var adminTokenA = CreateToken(UserRole.PharmacyAdmin, pharmacyA.Id);

        var createBranchReq = new CreateBranchRequest(
            Name: "Hacked Branch",
            Address: "Illegal Addr",
            City: "Cairo",
            Latitude: 30.0,
            Longitude: 31.0,
            Phone: "010");

        var request = new HttpRequestMessage(HttpMethod.Post, $"/api/v1/pharmacies/{pharmacyB.Id}/branches")
        {
            Content = JsonContent.Create(createBranchReq)
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", adminTokenA);

        // Act: Attempt cross-tenant branch creation
        var response = await _client.SendAsync(request);

        // Assert: Multi-tenant boundary MUST reject with 403 Forbidden!
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task MultiTenancy_PharmacyAdmin_UpdatingOtherPharmacyProfile_Returns403Forbidden()
    {
        // Arrange
        var (pharmacyA, pharmacyB) = await SeedTwoPharmaciesAsync();
        var adminTokenA = CreateToken(UserRole.PharmacyAdmin, pharmacyA.Id);

        var updateReq = new UpdatePharmacyProfileRequest("Tampered Name", "Tampered Addr", "Cairo", "010", null, null);
        var request = new HttpRequestMessage(HttpMethod.Put, $"/api/v1/pharmacies/{pharmacyB.Id}/profile")
        {
            Content = JsonContent.Create(updateReq)
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", adminTokenA);

        // Act
        var response = await _client.SendAsync(request);

        // Assert
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task MultiTenancy_PharmacyStaff_CanToggleOwnBranchStatus_ButCannotToggleOtherPharmacy()
    {
        // Arrange
        var (pharmacyA, pharmacyB) = await SeedTwoPharmaciesAsync();
        var staffTokenA = CreateToken(UserRole.PharmacyStaff, pharmacyA.Id);
        var branchA = pharmacyA.Branches.First();
        var branchB = pharmacyB.Branches.First();

        // Act 1: Toggle own pharmacy branch status -> Succeeds
        var toggleOwnReq = new HttpRequestMessage(HttpMethod.Patch, $"/api/v1/pharmacies/{pharmacyA.Id}/branches/{branchA.Id}/status")
        {
            Content = JsonContent.Create(new UpdateBranchStatusRequest(false))
        };
        toggleOwnReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", staffTokenA);
        var ownResp = await _client.SendAsync(toggleOwnReq);

        Assert.Equal(HttpStatusCode.OK, ownResp.StatusCode);
        var updated = await ownResp.Content.ReadFromJsonAsync<PharmacyBranchDto>();
        Assert.False(updated!.IsActive);

        // Act 2: Attempt to toggle Pharmacy B's branch status -> Rejected with 403
        var toggleOtherReq = new HttpRequestMessage(HttpMethod.Patch, $"/api/v1/pharmacies/{pharmacyB.Id}/branches/{branchB.Id}/status")
        {
            Content = JsonContent.Create(new UpdateBranchStatusRequest(false))
        };
        toggleOtherReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", staffTokenA);
        var otherResp = await _client.SendAsync(toggleOtherReq);

        Assert.Equal(HttpStatusCode.Forbidden, otherResp.StatusCode);
    }

    [Fact]
    public async Task PharmacyAdmin_AddStaff_CreatesStaffUserUnderSamePharmacy()
    {
        // Arrange
        var (pharmacyA, _) = await SeedTwoPharmaciesAsync();
        var adminTokenA = CreateToken(UserRole.PharmacyAdmin, pharmacyA.Id);

        var staffEmail = $"pharmacist_{Guid.NewGuid():N}@pharmaA.com";
        var staffReq = new InviteStaffRequest(
            Email: staffEmail,
            Password: "StaffPassword123!",
            FullName: "Pharmacist Mona",
            PhoneNumber: "+201012345678",
            Role: "PharmacyStaff");

        var request = new HttpRequestMessage(HttpMethod.Post, $"/api/v1/pharmacies/{pharmacyA.Id}/staff")
        {
            Content = JsonContent.Create(staffReq)
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", adminTokenA);

        // Act
        var response = await _client.SendAsync(request);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var staff = await response.Content.ReadFromJsonAsync<PharmacyStaffDto>();
        Assert.NotNull(staff);
        Assert.Equal(staffEmail.ToLowerInvariant(), staff.Email);
        Assert.Equal("PharmacyStaff", staff.Role);
        Assert.Equal(pharmacyA.Id, staff.PharmacyId);

        // Verify staff list returns newly added member
        var listReq = new HttpRequestMessage(HttpMethod.Get, $"/api/v1/pharmacies/{pharmacyA.Id}/staff");
        listReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", adminTokenA);
        var listResp = await _client.SendAsync(listReq);
        var staffList = await listResp.Content.ReadFromJsonAsync<List<PharmacyStaffDto>>();
        Assert.Contains(staffList!, s => s.Email == staffEmail.ToLowerInvariant());
    }
}
