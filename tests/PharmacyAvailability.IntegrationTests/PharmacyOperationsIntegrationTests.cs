using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PharmacyAvailability.Application.Common.Interfaces;
using PharmacyAvailability.Contracts.Catalog;
using PharmacyAvailability.Contracts.Operations;
using PharmacyAvailability.Domain.Catalog;
using PharmacyAvailability.Domain.Identity;
using PharmacyAvailability.Domain.Inventory;
using PharmacyAvailability.Domain.Pharmacies;
using PharmacyAvailability.Domain.Reservations;
using PharmacyAvailability.Infrastructure.Persistence;
using Xunit;

namespace PharmacyAvailability.IntegrationTests;

public class PharmacyOperationsIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public PharmacyOperationsIntegrationTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    private (string Token, User User) CreateTokenAndUser(UserRole role, Guid? pharmacyId = null, string? phone = null, string? name = null)
    {
        using var scope = _factory.Services.CreateScope();
        var tokenService = scope.ServiceProvider.GetRequiredService<ITokenService>();
        var db = scope.ServiceProvider.GetRequiredService<PharmacyDbContext>();

        var user = new User(
            $"user_{Guid.NewGuid():N}@test.com",
            "hash",
            name ?? $"User_{Guid.NewGuid():N}",
            role,
            phone ?? "+2010" + Random.Shared.Next(1000000, 9999999),
            pharmacyId);

        db.Users.Add(user);
        db.SaveChanges();

        var (token, _) = tokenService.GenerateAccessToken(user);
        return (token, user);
    }

    private async Task<(Pharmacy Pharmacy, PharmacyBranch Branch, Medicine Medicine, PharmacyInventory Inventory)> SeedBranchDataAsync(
        int availableQuantity = 20,
        decimal price = 45.00m)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PharmacyDbContext>();

        var cat = new Category($"Cat_{Guid.NewGuid():N}");
        var man = new Manufacturer($"Man_{Guid.NewGuid():N}");
        db.Categories.Add(cat);
        db.Manufacturers.Add(man);
        await db.SaveChangesAsync();

        var med = new Medicine(
            brandName: $"Med_{Guid.NewGuid():N}",
            genericName: "GenericMed",
            activeIngredient: "ActiveIng",
            strength: "250mg",
            dosageForm: "Capsule",
            categoryId: cat.Id,
            manufacturerId: man.Id);
        db.Medicines.Add(med);

        var pharm = new Pharmacy(
            name: $"Pharm_{Guid.NewGuid():N}",
            licenseNumber: $"LIC_{Guid.NewGuid():N}",
            address: "15 Zamalek St",
            city: "Cairo",
            coordinates: new Coordinates(30.05, 31.22),
            phone: "+20101",
            initialStatus: PharmacyStatus.Active);

        var branch = pharm.AddBranch(
            name: "Downtown Branch",
            address: "15 Zamalek St",
            city: "Cairo",
            coordinates: new Coordinates(30.05, 31.22),
            phone: "+20101");

        db.Pharmacies.Add(pharm);
        await db.SaveChangesAsync();

        var inv = new PharmacyInventory(pharm.Id, branch.Id, med.Id, availableQuantity, price);
        db.PharmacyInventories.Add(inv);
        await db.SaveChangesAsync();

        return (pharm, branch, med, inv);
    }

    [Fact]
    public async Task GetBranchDashboard_AuthorizedStaff_ReturnsAccurateCounters()
    {
        // Arrange
        var (pharmacy, branch, medicine, inv) = await SeedBranchDataAsync(availableQuantity: 3); // <= 5 is low stock
        var (staffToken, _) = CreateTokenAndUser(UserRole.PharmacyStaff, pharmacy.Id);
        var (_, customer) = CreateTokenAndUser(UserRole.Customer);

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<PharmacyDbContext>();

            // Active reservation expiring in 20 minutes (expiring soon)
            var resExpiringSoon = new Reservation(
                customer.Id, pharmacy.Id, branch.Id, medicine.Id, 1, DateTime.UtcNow.AddMinutes(20));
            db.Reservations.Add(resExpiringSoon);

            // Active reservation expiring in 90 minutes
            var resNormal = new Reservation(
                customer.Id, pharmacy.Id, branch.Id, medicine.Id, 1, DateTime.UtcNow.AddMinutes(90));
            db.Reservations.Add(resNormal);

            // Confirmed today
            var resConfirmed = new Reservation(
                customer.Id, pharmacy.Id, branch.Id, medicine.Id, 1, DateTime.UtcNow.AddMinutes(60));
            resConfirmed.Confirm();
            db.Reservations.Add(resConfirmed);

            await db.SaveChangesAsync();
        }

        // Act
        var req = new HttpRequestMessage(
            HttpMethod.Get,
            $"/api/v1/pharmacies/{pharmacy.Id}/branches/{branch.Id}/operations/dashboard");
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", staffToken);
        var resp = await _client.SendAsync(req);

        // Assert
        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        var dashboard = await resp.Content.ReadFromJsonAsync<BranchDashboardDto>();
        Assert.NotNull(dashboard);
        Assert.Equal(pharmacy.Id, dashboard.PharmacyId);
        Assert.Equal(branch.Id, dashboard.BranchId);
        Assert.True(dashboard.ActiveReservationsCount >= 2);
        Assert.True(dashboard.ExpiringSoonReservationsCount >= 1);
        Assert.True(dashboard.TodayConfirmedPickupsCount >= 1);
        Assert.True(dashboard.LowStockItemsCount >= 1);
        Assert.True(dashboard.TotalActiveMedicinesCount >= 1);
    }

    [Fact]
    public async Task GetStaffReservationQueue_CustomerSearch_FindsByPhoneOrName()
    {
        // Arrange
        var (pharmacy, branch, medicine, _) = await SeedBranchDataAsync(availableQuantity: 20);
        var (staffToken, _) = CreateTokenAndUser(UserRole.PharmacyStaff, pharmacy.Id);

        var targetPhone = "01099887766";
        var (_, custA) = CreateTokenAndUser(UserRole.Customer, phone: targetPhone, name: "Youssef Zaki");
        var (_, custB) = CreateTokenAndUser(UserRole.Customer, phone: "01233445566", name: "Sarah Nader");

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<PharmacyDbContext>();
            var resA = new Reservation(custA.Id, pharmacy.Id, branch.Id, medicine.Id, 2, DateTime.UtcNow.AddHours(2));
            var resB = new Reservation(custB.Id, pharmacy.Id, branch.Id, medicine.Id, 1, DateTime.UtcNow.AddHours(2));
            db.Reservations.AddRange(resA, resB);
            await db.SaveChangesAsync();
        }

        // Act 1: Search by phone
        var reqPhone = new HttpRequestMessage(
            HttpMethod.Get,
            $"/api/v1/pharmacies/{pharmacy.Id}/branches/{branch.Id}/operations/reservations/queue?search={targetPhone}");
        reqPhone.Headers.Authorization = new AuthenticationHeaderValue("Bearer", staffToken);
        var respPhone = await _client.SendAsync(reqPhone);

        // Assert 1
        Assert.Equal(HttpStatusCode.OK, respPhone.StatusCode);
        var resultPhone = await respPhone.Content.ReadFromJsonAsync<PagedResult<StaffReservationQueueItemDto>>();
        Assert.NotNull(resultPhone);
        Assert.Single(resultPhone.Items);
        Assert.Equal(custA.Id, resultPhone.Items[0].CustomerId);
        Assert.Equal(targetPhone, resultPhone.Items[0].CustomerPhone);

        // Act 2: Search by customer name
        var reqName = new HttpRequestMessage(
            HttpMethod.Get,
            $"/api/v1/pharmacies/{pharmacy.Id}/branches/{branch.Id}/operations/reservations/queue?search=Sarah");
        reqName.Headers.Authorization = new AuthenticationHeaderValue("Bearer", staffToken);
        var respName = await _client.SendAsync(reqName);

        // Assert 2
        Assert.Equal(HttpStatusCode.OK, respName.StatusCode);
        var resultName = await respName.Content.ReadFromJsonAsync<PagedResult<StaffReservationQueueItemDto>>();
        Assert.NotNull(resultName);
        Assert.Single(resultName.Items);
        Assert.Equal(custB.Id, resultName.Items[0].CustomerId);
    }

    [Fact]
    public async Task GetStaffReservationQueue_ExpiringSoonOnly_ReturnsUrgentHolds()
    {
        // Arrange
        var (pharmacy, branch, medicine, _) = await SeedBranchDataAsync(availableQuantity: 20);
        var (staffToken, _) = CreateTokenAndUser(UserRole.PharmacyStaff, pharmacy.Id);
        var (_, customer) = CreateTokenAndUser(UserRole.Customer);

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<PharmacyDbContext>();
            var resUrgent = new Reservation(customer.Id, pharmacy.Id, branch.Id, medicine.Id, 2, DateTime.UtcNow.AddMinutes(15));
            var resNormal = new Reservation(customer.Id, pharmacy.Id, branch.Id, medicine.Id, 1, DateTime.UtcNow.AddMinutes(110));
            db.Reservations.AddRange(resUrgent, resNormal);
            await db.SaveChangesAsync();
        }

        // Act
        var req = new HttpRequestMessage(
            HttpMethod.Get,
            $"/api/v1/pharmacies/{pharmacy.Id}/branches/{branch.Id}/operations/reservations/queue?expiringSoonOnly=true");
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", staffToken);
        var resp = await _client.SendAsync(req);

        // Assert
        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        var result = await resp.Content.ReadFromJsonAsync<PagedResult<StaffReservationQueueItemDto>>();
        Assert.NotNull(result);
        Assert.All(result.Items, item =>
        {
            Assert.True(item.IsExpiringSoon);
            Assert.True(item.RemainingMinutes <= 30);
        });
    }

    [Fact]
    public async Task GetLowStockAlerts_FiltersByThreshold_ReturnsSortedByAvailableAscending()
    {
        // Arrange
        var (pharmacy, branch, med1, inv1) = await SeedBranchDataAsync(availableQuantity: 2);
        var (staffToken, _) = CreateTokenAndUser(UserRole.PharmacyStaff, pharmacy.Id);

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<PharmacyDbContext>();
            var cat = await db.Categories.FirstAsync();
            var man = await db.Manufacturers.FirstAsync();

            var med2 = new Medicine("MedLow5", "Gen5", "Act5", "100mg", "Tab", cat.Id, man.Id);
            var med3 = new Medicine("MedHigh50", "Gen50", "Act50", "100mg", "Tab", cat.Id, man.Id);
            db.Medicines.AddRange(med2, med3);
            await db.SaveChangesAsync();

            var inv2 = new PharmacyInventory(pharmacy.Id, branch.Id, med2.Id, 5, 20m);
            var inv3 = new PharmacyInventory(pharmacy.Id, branch.Id, med3.Id, 50, 20m);
            db.PharmacyInventories.AddRange(inv2, inv3);
            await db.SaveChangesAsync();
        }

        // Act: Query low stock with threshold = 5
        var req = new HttpRequestMessage(
            HttpMethod.Get,
            $"/api/v1/pharmacies/{pharmacy.Id}/branches/{branch.Id}/operations/inventory/low-stock?threshold=5");
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", staffToken);
        var resp = await _client.SendAsync(req);

        // Assert
        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        var result = await resp.Content.ReadFromJsonAsync<PagedResult<LowStockAlertDto>>();
        Assert.NotNull(result);
        Assert.True(result.Items.Count >= 2);
        Assert.All(result.Items, alert => Assert.True(alert.AvailableQuantity <= 5));

        // Ascending order check: 2 <= 5
        for (int i = 0; i < result.Items.Count - 1; i++)
        {
            Assert.True(result.Items[i].AvailableQuantity <= result.Items[i + 1].AvailableQuantity);
        }
    }

    [Fact]
    public async Task GetBranchActivityFeed_ReturnsRecentLedgerEvents()
    {
        // Arrange
        var (pharmacy, branch, medicine, inv) = await SeedBranchDataAsync(availableQuantity: 20);
        var (staffToken, staff) = CreateTokenAndUser(UserRole.PharmacyStaff, pharmacy.Id);

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<PharmacyDbContext>();
            var trackedInv = await db.PharmacyInventories.FindAsync(inv.Id);

            // Create some inventory adjustments
            trackedInv!.AdjustStock(10, InventoryAdjustmentReason.Restock, staff.Id, "Bulk supplier delivery");
            trackedInv.Reserve(3, staff.Id, "Counter customer hold");
            await db.SaveChangesAsync();
        }

        // Act
        var req = new HttpRequestMessage(
            HttpMethod.Get,
            $"/api/v1/pharmacies/{pharmacy.Id}/branches/{branch.Id}/operations/activity");
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", staffToken);
        var resp = await _client.SendAsync(req);

        // Assert
        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        var result = await resp.Content.ReadFromJsonAsync<PagedResult<BranchActivityFeedItemDto>>();
        Assert.NotNull(result);
        Assert.True(result.Items.Count >= 2);
        Assert.Contains(result.Items, item => item.ActivityType == "Restock");
        Assert.Contains(result.Items, item => item.ActivityType == "ReservationHold");
    }

    [Fact]
    public async Task MultiTenant_StaffFromDifferentPharmacy_CannotAccessOperations_Returns403()
    {
        // Arrange
        var (pharmacyA, branchA, _, _) = await SeedBranchDataAsync();
        var (pharmacyB, _, _, _) = await SeedBranchDataAsync();

        // Staff belonging to Pharmacy B
        var (staffBToken, _) = CreateTokenAndUser(UserRole.PharmacyStaff, pharmacyB.Id);

        // Act: Staff B tries to access Pharmacy A's operations dashboard
        var req = new HttpRequestMessage(
            HttpMethod.Get,
            $"/api/v1/pharmacies/{pharmacyA.Id}/branches/{branchA.Id}/operations/dashboard");
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", staffBToken);
        var resp = await _client.SendAsync(req);

        // Assert: 403 Forbidden
        Assert.Equal(HttpStatusCode.Forbidden, resp.StatusCode);
    }

    [Fact]
    public async Task MultiTenant_Customer_CannotAccessOperations_Returns403()
    {
        // Arrange
        var (pharmacy, branch, _, _) = await SeedBranchDataAsync();
        var (custToken, _) = CreateTokenAndUser(UserRole.Customer);

        // Act: Customer tries to access staff dashboard
        var req = new HttpRequestMessage(
            HttpMethod.Get,
            $"/api/v1/pharmacies/{pharmacy.Id}/branches/{branch.Id}/operations/dashboard");
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", custToken);
        var resp = await _client.SendAsync(req);

        // Assert: 403 Forbidden
        Assert.Equal(HttpStatusCode.Forbidden, resp.StatusCode);
    }

    [Fact]
    public async Task PlatformAdmin_CanAccessAnyPharmacyBranchOperations()
    {
        // Arrange
        var (pharmacy, branch, _, _) = await SeedBranchDataAsync();
        var (adminToken, _) = CreateTokenAndUser(UserRole.PlatformAdmin);

        // Act: PlatformAdmin accesses Pharmacy A's dashboard
        var req = new HttpRequestMessage(
            HttpMethod.Get,
            $"/api/v1/pharmacies/{pharmacy.Id}/branches/{branch.Id}/operations/dashboard");
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);
        var resp = await _client.SendAsync(req);

        // Assert: 200 OK
        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        var dashboard = await resp.Content.ReadFromJsonAsync<BranchDashboardDto>();
        Assert.NotNull(dashboard);
        Assert.Equal(pharmacy.Id, dashboard.PharmacyId);
    }

    [Fact]
    public async Task Unauthenticated_Returns401Unauthorized()
    {
        // Arrange
        var (pharmacy, branch, _, _) = await SeedBranchDataAsync();

        // Act: No Bearer token provided
        var req = new HttpRequestMessage(
            HttpMethod.Get,
            $"/api/v1/pharmacies/{pharmacy.Id}/branches/{branch.Id}/operations/dashboard");
        var resp = await _client.SendAsync(req);

        // Assert: 401 Unauthorized
        Assert.Equal(HttpStatusCode.Unauthorized, resp.StatusCode);
    }

    [Fact]
    public async Task GetStaffReservationQueue_OverdueActiveReservation_ExcludedFromActiveQueue()
    {
        // Arrange
        var (pharmacy, branch, medicine, _) = await SeedBranchDataAsync();
        var (staffToken, _) = CreateTokenAndUser(UserRole.PharmacyStaff, pharmacy.Id);
        var (_, customer) = CreateTokenAndUser(UserRole.Customer);

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<PharmacyDbContext>();
            var overdueRes = new Reservation(
                customer.Id,
                pharmacy.Id,
                branch.Id,
                medicine.Id,
                quantity: 1,
                expiresAt: DateTime.UtcNow.AddMinutes(30));

            // Backdate CreatedAt to 60 minutes ago and ExpiresAt to 10 minutes ago (satisfies CK_Reservations_ExpiresAt: ExpiresAt > CreatedAt)
            typeof(Reservation).GetProperty("CreatedAt")!.SetValue(overdueRes, DateTime.UtcNow.AddMinutes(-60));
            typeof(Reservation).GetProperty("ExpiresAt")!.SetValue(overdueRes, DateTime.UtcNow.AddMinutes(-10));
            db.Reservations.Add(overdueRes);
            await db.SaveChangesAsync();
        }

        // Act: Query active queue
        var req = new HttpRequestMessage(
            HttpMethod.Get,
            $"/api/v1/pharmacies/{pharmacy.Id}/branches/{branch.Id}/operations/reservations/queue?status=Active");
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", staffToken);
        var resp = await _client.SendAsync(req);

        // Assert
        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        var result = await resp.Content.ReadFromJsonAsync<PagedResult<StaffReservationQueueItemDto>>();
        Assert.NotNull(result);
        Assert.Empty(result.Items);
    }

    [Fact]
    public async Task GetStaffReservationQueue_SearchByReservationIdGuid_ReturnsExactMatch()
    {
        // Arrange
        var (pharmacy, branch, medicine, _) = await SeedBranchDataAsync();
        var (staffToken, _) = CreateTokenAndUser(UserRole.PharmacyStaff, pharmacy.Id);
        var (_, customer) = CreateTokenAndUser(UserRole.Customer);

        Guid targetReservationId;
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<PharmacyDbContext>();
            var res1 = new Reservation(customer.Id, pharmacy.Id, branch.Id, medicine.Id, 2, DateTime.UtcNow.AddHours(1));
            var res2 = new Reservation(customer.Id, pharmacy.Id, branch.Id, medicine.Id, 3, DateTime.UtcNow.AddHours(1));
            db.Reservations.AddRange(res1, res2);
            await db.SaveChangesAsync();
            targetReservationId = res1.Id;
        }

        // Act: Search by exact reservation GUID
        var req = new HttpRequestMessage(
            HttpMethod.Get,
            $"/api/v1/pharmacies/{pharmacy.Id}/branches/{branch.Id}/operations/reservations/queue?search={targetReservationId}");
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", staffToken);
        var resp = await _client.SendAsync(req);

        // Assert
        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        var result = await resp.Content.ReadFromJsonAsync<PagedResult<StaffReservationQueueItemDto>>();
        Assert.NotNull(result);
        Assert.Single(result.Items);
        Assert.Equal(targetReservationId, result.Items[0].ReservationId);
    }

    [Fact]
    public async Task GetLowStockAlerts_InactiveMedicine_IsExcluded()
    {
        // Arrange
        var (pharmacy, branch, activeMed, _) = await SeedBranchDataAsync(availableQuantity: 2);
        var (staffToken, _) = CreateTokenAndUser(UserRole.PharmacyStaff, pharmacy.Id);

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<PharmacyDbContext>();
            var cat = await db.Categories.FirstAsync();
            var man = await db.Manufacturers.FirstAsync();

            var inactiveMed = new Medicine("InactiveMed", "GenInactive", "Act", "10mg", "Tab", cat.Id, man.Id);
            inactiveMed.Deactivate();
            db.Medicines.Add(inactiveMed);
            await db.SaveChangesAsync();

            var inactiveInv = new PharmacyInventory(pharmacy.Id, branch.Id, inactiveMed.Id, 1, 10m);
            db.PharmacyInventories.Add(inactiveInv);
            await db.SaveChangesAsync();
        }

        // Act
        var req = new HttpRequestMessage(
            HttpMethod.Get,
            $"/api/v1/pharmacies/{pharmacy.Id}/branches/{branch.Id}/operations/inventory/low-stock?threshold=5");
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", staffToken);
        var resp = await _client.SendAsync(req);

        // Assert
        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        var result = await resp.Content.ReadFromJsonAsync<PagedResult<LowStockAlertDto>>();
        Assert.NotNull(result);
        Assert.DoesNotContain(result.Items, i => i.MedicineBrandName == "InactiveMed");
        Assert.Contains(result.Items, i => i.MedicineId == activeMed.Id);
    }

    [Fact]
    public async Task BranchBelongingToAnotherPharmacy_Returns404NotFound()
    {
        // Arrange
        var (pharmacyA, branchA, _, _) = await SeedBranchDataAsync();
        var (pharmacyB, branchB, _, _) = await SeedBranchDataAsync();

        // Admin of Pharmacy A accesses branch B under route of pharmacy A
        var (adminAToken, _) = CreateTokenAndUser(UserRole.PharmacyAdmin, pharmacyA.Id);

        // Act: route has pharmacyA.Id but branchB.Id (which belongs to pharmacyB)
        var req = new HttpRequestMessage(
            HttpMethod.Get,
            $"/api/v1/pharmacies/{pharmacyA.Id}/branches/{branchB.Id}/operations/dashboard");
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", adminAToken);
        var resp = await _client.SendAsync(req);

        // Assert: 404 Not Found (branch does not belong to pharmacyA)
        Assert.Equal(HttpStatusCode.NotFound, resp.StatusCode);
    }
}
