using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PharmacyAvailability.Application.Common.Interfaces;
using PharmacyAvailability.Application.Reservations;
using PharmacyAvailability.Contracts.Catalog;
using PharmacyAvailability.Contracts.Reservations;
using PharmacyAvailability.Domain.Catalog;
using PharmacyAvailability.Domain.Identity;
using PharmacyAvailability.Domain.Inventory;
using PharmacyAvailability.Domain.Pharmacies;
using PharmacyAvailability.Domain.Reservations;
using PharmacyAvailability.Infrastructure.Persistence;
using Xunit;

namespace PharmacyAvailability.IntegrationTests;

public class ReservationIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public ReservationIntegrationTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    private (string Token, User User) CreateTokenAndUser(UserRole role, Guid? pharmacyId = null)
    {
        using var scope = _factory.Services.CreateScope();
        var tokenService = scope.ServiceProvider.GetRequiredService<ITokenService>();
        var db = scope.ServiceProvider.GetRequiredService<PharmacyDbContext>();

        var user = new User($"user_{Guid.NewGuid():N}@test.com", "hash", "Test User", role, "+2010", pharmacyId);
        db.Users.Add(user);
        db.SaveChanges();

        var (token, _) = tokenService.GenerateAccessToken(user);
        return (token, user);
    }

    private async Task<(Pharmacy Pharmacy, PharmacyBranch Branch, Medicine Medicine, PharmacyInventory Inventory)> SeedInventoryAsync(
        int availableQuantity = 20,
        decimal price = 50.00m,
        PharmacyStatus pharmacyStatus = PharmacyStatus.Active,
        bool branchIsActive = true,
        bool medicineIsActive = true)
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
            genericName: "Generic",
            activeIngredient: "Active",
            strength: "500mg",
            dosageForm: "Tablet",
            categoryId: cat.Id,
            manufacturerId: man.Id);

        if (!medicineIsActive)
            med.Deactivate();

        db.Medicines.Add(med);

        var pharm = new Pharmacy(
            name: $"Pharm_{Guid.NewGuid():N}",
            licenseNumber: $"LIC_{Guid.NewGuid():N}",
            address: "10 Nile St",
            city: "Cairo",
            coordinates: new Coordinates(30.0, 31.0),
            phone: "+20100",
            initialStatus: pharmacyStatus);

        var branch = pharm.AddBranch(
            name: "Main Branch",
            address: "10 Nile St",
            city: "Cairo",
            coordinates: new Coordinates(30.0, 31.0),
            phone: "+20100");

        if (!branchIsActive)
            branch.Deactivate();

        db.Pharmacies.Add(pharm);
        await db.SaveChangesAsync();

        var inv = new PharmacyInventory(pharm.Id, branch.Id, med.Id, availableQuantity, price);
        db.PharmacyInventories.Add(inv);
        await db.SaveChangesAsync();

        return (pharm, branch, med, inv);
    }

    [Fact]
    public async Task CreateReservation_Customer_Succeeds_ReducesAvailableAndIncreasesReserved()
    {
        // Arrange
        var (pharmacy, branch, medicine, _) = await SeedInventoryAsync(availableQuantity: 20);
        var (token, customer) = CreateTokenAndUser(UserRole.Customer);

        var request = new CreateReservationRequest(branch.Id, medicine.Id, 5);
        var httpReq = new HttpRequestMessage(HttpMethod.Post, "/api/v1/reservations")
        {
            Content = JsonContent.Create(request)
        };
        httpReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await _client.SendAsync(httpReq);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var reservation = await response.Content.ReadFromJsonAsync<ReservationDto>();
        Assert.NotNull(reservation);
        Assert.Equal(customer.Id, reservation.CustomerId);
        Assert.Equal(branch.Id, reservation.BranchId);
        Assert.Equal(medicine.Id, reservation.MedicineId);
        Assert.Equal(5, reservation.Quantity);
        Assert.Equal("Active", reservation.Status);
        Assert.True(reservation.ExpiresAt > reservation.CreatedAt);

        // Verify Inventory state
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PharmacyDbContext>();
        var updatedInv = await db.PharmacyInventories.FirstOrDefaultAsync(pi => pi.BranchId == branch.Id && pi.MedicineId == medicine.Id);
        Assert.NotNull(updatedInv);
        Assert.Equal(15, updatedInv.AvailableQuantity);
        Assert.Equal(5, updatedInv.ReservedQuantity);

        // Verify ledger
        var tx = db.InventoryTransactions.FirstOrDefault(t => t.PharmacyInventoryId == updatedInv.Id);
        Assert.NotNull(tx);
        Assert.Equal(-5, tx.DeltaQuantity);
        Assert.Equal(InventoryAdjustmentReason.ReservationHold, tx.Reason);
    }

    [Fact]
    public async Task CreateReservation_Unauthenticated_Returns401Unauthorized()
    {
        var request = new CreateReservationRequest(Guid.NewGuid(), Guid.NewGuid(), 1);
        var response = await _client.PostAsJsonAsync("/api/v1/reservations", request);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task CreateReservation_InsufficientStock_Returns409Conflict()
    {
        // Arrange: 3 units available
        var (_, branch, medicine, _) = await SeedInventoryAsync(availableQuantity: 3);
        var (token, _) = CreateTokenAndUser(UserRole.Customer);

        // Act: Attempt to reserve 10 units
        var request = new CreateReservationRequest(branch.Id, medicine.Id, 10);
        var httpReq = new HttpRequestMessage(HttpMethod.Post, "/api/v1/reservations")
        {
            Content = JsonContent.Create(request)
        };
        httpReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _client.SendAsync(httpReq);

        // Assert
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task CreateReservation_InactiveBranch_Returns400BadRequest()
    {
        // Arrange: Inactive branch
        var (_, branch, medicine, _) = await SeedInventoryAsync(branchIsActive: false);
        var (token, _) = CreateTokenAndUser(UserRole.Customer);

        var request = new CreateReservationRequest(branch.Id, medicine.Id, 1);
        var httpReq = new HttpRequestMessage(HttpMethod.Post, "/api/v1/reservations")
        {
            Content = JsonContent.Create(request)
        };
        httpReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _client.SendAsync(httpReq);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CancelReservation_ByOwningCustomer_RestoresAvailableStock()
    {
        // Arrange
        var (_, branch, medicine, _) = await SeedInventoryAsync(availableQuantity: 20);
        var (token, _) = CreateTokenAndUser(UserRole.Customer);

        // Create reservation of 5
        var createReq = new HttpRequestMessage(HttpMethod.Post, "/api/v1/reservations")
        {
            Content = JsonContent.Create(new CreateReservationRequest(branch.Id, medicine.Id, 5))
        };
        createReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var createResp = await _client.SendAsync(createReq);
        var created = await createResp.Content.ReadFromJsonAsync<ReservationDto>();

        // Act: Customer cancels reservation
        var cancelReq = new HttpRequestMessage(HttpMethod.Post, $"/api/v1/reservations/{created!.Id}/cancel");
        cancelReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var cancelResp = await _client.SendAsync(cancelReq);

        // Assert
        Assert.Equal(HttpStatusCode.OK, cancelResp.StatusCode);
        var cancelled = await cancelResp.Content.ReadFromJsonAsync<ReservationDto>();
        Assert.Equal("Cancelled", cancelled!.Status);
        Assert.NotNull(cancelled.CancelledAt);

        // Verify inventory restored
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PharmacyDbContext>();
        var updatedInv = await db.PharmacyInventories.FirstOrDefaultAsync(pi => pi.BranchId == branch.Id && pi.MedicineId == medicine.Id);
        Assert.NotNull(updatedInv);
        Assert.Equal(20, updatedInv.AvailableQuantity);
        Assert.Equal(0, updatedInv.ReservedQuantity);
    }

    [Fact]
    public async Task CancelReservation_ByDifferentCustomer_Returns404NotFound()
    {
        // Arrange
        var (_, branch, medicine, _) = await SeedInventoryAsync(availableQuantity: 20);
        var (tokenA, _) = CreateTokenAndUser(UserRole.Customer);
        var (tokenB, _) = CreateTokenAndUser(UserRole.Customer);

        var createReq = new HttpRequestMessage(HttpMethod.Post, "/api/v1/reservations")
        {
            Content = JsonContent.Create(new CreateReservationRequest(branch.Id, medicine.Id, 2))
        };
        createReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", tokenA);
        var createResp = await _client.SendAsync(createReq);
        var created = await createResp.Content.ReadFromJsonAsync<ReservationDto>();

        // Act: Customer B attempts to cancel Customer A's reservation
        var cancelReq = new HttpRequestMessage(HttpMethod.Post, $"/api/v1/reservations/{created!.Id}/cancel");
        cancelReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", tokenB);
        var cancelResp = await _client.SendAsync(cancelReq);

        // Assert: 404 to avoid leaking existence
        Assert.Equal(HttpStatusCode.NotFound, cancelResp.StatusCode);
    }

    [Fact]
    public async Task CancelReservation_RepeatedCancellation_Returns409Conflict()
    {
        // Arrange
        var (_, branch, medicine, _) = await SeedInventoryAsync(availableQuantity: 10);
        var (token, _) = CreateTokenAndUser(UserRole.Customer);

        var createReq = new HttpRequestMessage(HttpMethod.Post, "/api/v1/reservations")
        {
            Content = JsonContent.Create(new CreateReservationRequest(branch.Id, medicine.Id, 2))
        };
        createReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var createResp = await _client.SendAsync(createReq);
        var created = await createResp.Content.ReadFromJsonAsync<ReservationDto>();

        // Cancel once -> 200 OK
        var cancelReq1 = new HttpRequestMessage(HttpMethod.Post, $"/api/v1/reservations/{created!.Id}/cancel");
        cancelReq1.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var cancelResp1 = await _client.SendAsync(cancelReq1);
        Assert.Equal(HttpStatusCode.OK, cancelResp1.StatusCode);

        // Act: Cancel second time
        var cancelReq2 = new HttpRequestMessage(HttpMethod.Post, $"/api/v1/reservations/{created.Id}/cancel");
        cancelReq2.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var cancelResp2 = await _client.SendAsync(cancelReq2);

        // Assert: 409 Conflict
        Assert.Equal(HttpStatusCode.Conflict, cancelResp2.StatusCode);
    }

    [Fact]
    public async Task ConfirmReservation_ByAuthorizedPharmacyStaff_ConsumesReservedStock()
    {
        // Arrange
        var (pharmacy, branch, medicine, _) = await SeedInventoryAsync(availableQuantity: 20);
        var (custToken, _) = CreateTokenAndUser(UserRole.Customer);
        var (staffToken, _) = CreateTokenAndUser(UserRole.PharmacyStaff, pharmacy.Id);

        // Customer creates reservation of 4
        var createReq = new HttpRequestMessage(HttpMethod.Post, "/api/v1/reservations")
        {
            Content = JsonContent.Create(new CreateReservationRequest(branch.Id, medicine.Id, 4))
        };
        createReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", custToken);
        var createResp = await _client.SendAsync(createReq);
        var created = await createResp.Content.ReadFromJsonAsync<ReservationDto>();

        // Act: Pharmacy staff confirms reservation
        var confirmReq = new HttpRequestMessage(HttpMethod.Post, $"/api/v1/reservations/{created!.Id}/confirm");
        confirmReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", staffToken);
        var confirmResp = await _client.SendAsync(confirmReq);

        // Assert
        Assert.Equal(HttpStatusCode.OK, confirmResp.StatusCode);
        var confirmed = await confirmResp.Content.ReadFromJsonAsync<ReservationDto>();
        Assert.Equal("Confirmed", confirmed!.Status);
        Assert.NotNull(confirmed.ConfirmedAt);

        // Verify inventory state: Available remains 16, Reserved becomes 0
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PharmacyDbContext>();
        var updatedInv = await db.PharmacyInventories.FirstOrDefaultAsync(pi => pi.BranchId == branch.Id && pi.MedicineId == medicine.Id);
        Assert.NotNull(updatedInv);
        Assert.Equal(16, updatedInv.AvailableQuantity);
        Assert.Equal(0, updatedInv.ReservedQuantity);
    }

    [Fact]
    public async Task ConfirmReservation_ByStaffOfDifferentPharmacy_Returns403Forbidden()
    {
        // Arrange
        var (pharmacyA, branchA, medicine, _) = await SeedInventoryAsync(availableQuantity: 20);
        var (pharmacyB, _, _, _) = await SeedInventoryAsync(availableQuantity: 20);

        var (custToken, _) = CreateTokenAndUser(UserRole.Customer);
        var (staffBToken, _) = CreateTokenAndUser(UserRole.PharmacyStaff, pharmacyB.Id);

        var createReq = new HttpRequestMessage(HttpMethod.Post, "/api/v1/reservations")
        {
            Content = JsonContent.Create(new CreateReservationRequest(branchA.Id, medicine.Id, 2))
        };
        createReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", custToken);
        var createResp = await _client.SendAsync(createReq);
        var created = await createResp.Content.ReadFromJsonAsync<ReservationDto>();

        // Act: Staff of Pharmacy B tries to confirm Pharmacy A's reservation
        var confirmReq = new HttpRequestMessage(HttpMethod.Post, $"/api/v1/reservations/{created!.Id}/confirm");
        confirmReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", staffBToken);
        var confirmResp = await _client.SendAsync(confirmReq);

        // Assert: 403 Forbidden
        Assert.Equal(HttpStatusCode.Forbidden, confirmResp.StatusCode);
    }

    [Fact]
    public async Task ExpirationSweep_OverdueReservation_RestoresAvailableStockAndIsIdempotent()
    {
        // Arrange
        var (pharmacy, branch, medicine, _) = await SeedInventoryAsync(availableQuantity: 20);
        var (_, customer) = CreateTokenAndUser(UserRole.Customer);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PharmacyDbContext>();
        var reservationService = scope.ServiceProvider.GetRequiredService<IReservationService>();

        // Hold 6 units
        var inv = await db.PharmacyInventories.FirstOrDefaultAsync(pi => pi.BranchId == branch.Id && pi.MedicineId == medicine.Id);
        inv!.Reserve(6, customer.Id, "Expired hold test");
        await db.SaveChangesAsync();

        // Create valid reservation then simulate time expiration
        var reservation = new Reservation(
            customer.Id,
            pharmacy.Id,
            branch.Id,
            medicine.Id,
            6,
            DateTime.UtcNow.AddMinutes(30));
        db.Reservations.Add(reservation);
        await db.SaveChangesAsync();

        // Simulate time passing: set CreatedAt to 3 hours ago and ExpiresAt to 1 hour ago (ExpiresAt > CreatedAt)
        await db.Database.ExecuteSqlRawAsync(
            "UPDATE Reservations SET CreatedAt = {0}, ExpiresAt = {1} WHERE Id = {2}",
            DateTime.UtcNow.AddHours(-3), DateTime.UtcNow.AddHours(-1), reservation.Id);

        // Verify pre-sweep state: 14 available, 6 reserved
        Assert.Equal(14, inv.AvailableQuantity);
        Assert.Equal(6, inv.ReservedQuantity);

        // Act 1: Execute sweep using fresh scope
        int expiredCount;
        using (var sweepScope = _factory.Services.CreateScope())
        {
            var sweepService = sweepScope.ServiceProvider.GetRequiredService<IReservationService>();
            expiredCount = await sweepService.ExpireOverdueReservationsAsync();
        }

        // Assert
        Assert.True(expiredCount >= 1);

        using var verifyScope = _factory.Services.CreateScope();
        var verifyDb = verifyScope.ServiceProvider.GetRequiredService<PharmacyDbContext>();
        var verifiedRes = await verifyDb.Reservations.FindAsync(reservation.Id);
        var verifiedInv = await verifyDb.PharmacyInventories.FirstOrDefaultAsync(pi => pi.BranchId == branch.Id && pi.MedicineId == medicine.Id);

        Assert.Equal(ReservationStatus.Expired, verifiedRes!.Status);
        Assert.NotNull(verifiedRes.ExpiredAt);
        Assert.Equal(20, verifiedInv!.AvailableQuantity);
        Assert.Equal(0, verifiedInv.ReservedQuantity);

        // Act 2: Second sweep (idempotency check)
        int secondSweepCount;
        using (var secondScope = _factory.Services.CreateScope())
        {
            var sweepService2 = secondScope.ServiceProvider.GetRequiredService<IReservationService>();
            secondSweepCount = await sweepService2.ExpireOverdueReservationsAsync();
        }
        Assert.Equal(0, secondSweepCount);
    }

    [Fact]
    public async Task ExpireOverdueReservations_WhenReservedQuantityIsZero_TransitionsToExpiredWithoutCrashing()
    {
        // Arrange
        var (pharmacy, branch, medicine, _) = await SeedInventoryAsync(availableQuantity: 20);
        var (_, customer) = CreateTokenAndUser(UserRole.Customer);

        Guid resId;
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<PharmacyDbContext>();
            // Reservation created with quantity 2, but inventory has 0 reserved units (simulating data discrepancy)
            var res = new Reservation(customer.Id, pharmacy.Id, branch.Id, medicine.Id, 2, DateTime.UtcNow.AddMinutes(30));
            typeof(Reservation).GetProperty("CreatedAt")!.SetValue(res, DateTime.UtcNow.AddMinutes(-60));
            typeof(Reservation).GetProperty("ExpiresAt")!.SetValue(res, DateTime.UtcNow.AddMinutes(-10));
            db.Reservations.Add(res);
            await db.SaveChangesAsync();
            resId = res.Id;
        }

        // Act: Run expiration sweep
        using (var sweepScope = _factory.Services.CreateScope())
        {
            var sweepService = sweepScope.ServiceProvider.GetRequiredService<IReservationService>();
            var count = await sweepService.ExpireOverdueReservationsAsync();
            Assert.True(count >= 1);
        }

        // Assert: Reservation transitioned to Expired without throwing DomainValidationException
        using (var verifyScope = _factory.Services.CreateScope())
        {
            var db = verifyScope.ServiceProvider.GetRequiredService<PharmacyDbContext>();
            var verifiedRes = await db.Reservations.FindAsync(resId);
            Assert.NotNull(verifiedRes);
            Assert.Equal(ReservationStatus.Expired, verifiedRes.Status);
            Assert.NotNull(verifiedRes.ExpiredAt);
        }
    }

    [Fact]
    public async Task ConcurrentReservations_SimultaneousRequests_PreventsOverselling()
    {
        // Arrange: 5 available units
        var (_, branch, medicine, _) = await SeedInventoryAsync(availableQuantity: 5);
        var (token1, _) = CreateTokenAndUser(UserRole.Customer);
        var (token2, _) = CreateTokenAndUser(UserRole.Customer);

        // Both customers request 4 units (4 + 4 = 8 > 5)
        var client1 = _factory.CreateClient();
        var client2 = _factory.CreateClient();

        Func<HttpClient, string, Task<HttpResponseMessage>> makeRequest = async (httpClient, jwtToken) =>
        {
            var req = new HttpRequestMessage(HttpMethod.Post, "/api/v1/reservations")
            {
                Content = JsonContent.Create(new CreateReservationRequest(branch.Id, medicine.Id, 4))
            };
            req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", jwtToken);
            return await httpClient.SendAsync(req);
        };

        // Act: Execute concurrently
        var task1 = makeRequest(client1, token1);
        var task2 = makeRequest(client2, token2);
        var responses = await Task.WhenAll(task1, task2);

        // Assert: Exactly one 201 Created and one 409 Conflict
        var createdCount = responses.Count(r => r.StatusCode == HttpStatusCode.Created);
        var conflictCount = responses.Count(r => r.StatusCode == HttpStatusCode.Conflict);

        Assert.Equal(1, createdCount);
        Assert.Equal(1, conflictCount);

        // Verify final inventory: 1 available, 4 reserved
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PharmacyDbContext>();
        var finalInv = await db.PharmacyInventories.FirstOrDefaultAsync(pi => pi.BranchId == branch.Id && pi.MedicineId == medicine.Id);
        Assert.NotNull(finalInv);
        Assert.Equal(1, finalInv.AvailableQuantity);
        Assert.Equal(4, finalInv.ReservedQuantity);
    }

    [Fact]
    public async Task GetReservations_CustomerScoping_OnlyReturnsOwnReservations()
    {
        // Arrange
        var (_, branch, medicine, _) = await SeedInventoryAsync(availableQuantity: 20);
        var (tokenA, custA) = CreateTokenAndUser(UserRole.Customer);
        var (tokenB, custB) = CreateTokenAndUser(UserRole.Customer);

        // Customer A creates reservation
        var reqA = new HttpRequestMessage(HttpMethod.Post, "/api/v1/reservations")
        {
            Content = JsonContent.Create(new CreateReservationRequest(branch.Id, medicine.Id, 2))
        };
        reqA.Headers.Authorization = new AuthenticationHeaderValue("Bearer", tokenA);
        await _client.SendAsync(reqA);

        // Customer B creates reservation
        var reqB = new HttpRequestMessage(HttpMethod.Post, "/api/v1/reservations")
        {
            Content = JsonContent.Create(new CreateReservationRequest(branch.Id, medicine.Id, 3))
        };
        reqB.Headers.Authorization = new AuthenticationHeaderValue("Bearer", tokenB);
        await _client.SendAsync(reqB);

        // Act: Customer A queries reservations
        var listReq = new HttpRequestMessage(HttpMethod.Get, "/api/v1/reservations");
        listReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", tokenA);
        var listResp = await _client.SendAsync(listReq);

        // Assert
        Assert.Equal(HttpStatusCode.OK, listResp.StatusCode);
        var result = await listResp.Content.ReadFromJsonAsync<PagedResult<ReservationDto>>();
        Assert.NotNull(result);
        Assert.All(result.Items, r => Assert.Equal(custA.Id, r.CustomerId));
    }

    [Fact]
    public async Task ConfirmReservation_AfterExpiration_Returns409AndTransitionsToExpired()
    {
        // Arrange: 20 available, customer reserves 5 -> 15 available, 5 reserved
        var (pharmacy, branch, medicine, _) = await SeedInventoryAsync(availableQuantity: 20);
        var (custToken, _) = CreateTokenAndUser(UserRole.Customer);
        var (staffToken, _) = CreateTokenAndUser(UserRole.PharmacyStaff, pharmacy.Id);

        var createReq = new HttpRequestMessage(HttpMethod.Post, "/api/v1/reservations")
        {
            Content = JsonContent.Create(new CreateReservationRequest(branch.Id, medicine.Id, 5))
        };
        createReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", custToken);
        var createResp = await _client.SendAsync(createReq);
        var created = await createResp.Content.ReadFromJsonAsync<ReservationDto>();

        // Simulate expiration: set CreatedAt to 3 hours ago and ExpiresAt to 1 hour ago
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<PharmacyDbContext>();
            await db.Database.ExecuteSqlRawAsync(
                "UPDATE Reservations SET CreatedAt = {0}, ExpiresAt = {1} WHERE Id = {2}",
                DateTime.UtcNow.AddHours(-3), DateTime.UtcNow.AddHours(-1), created!.Id);
        }

        // Act: Staff attempts to confirm the expired reservation
        var confirmReq = new HttpRequestMessage(HttpMethod.Post, $"/api/v1/reservations/{created!.Id}/confirm");
        confirmReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", staffToken);
        var confirmResp = await _client.SendAsync(confirmReq);

        // Assert: 409 Conflict
        Assert.Equal(HttpStatusCode.Conflict, confirmResp.StatusCode);

        // Verify that reservation transitioned to Expired and stock was restored to 20 available
        using (var verifyScope = _factory.Services.CreateScope())
        {
            var db = verifyScope.ServiceProvider.GetRequiredService<PharmacyDbContext>();
            var res = await db.Reservations.FindAsync(created.Id);
            var inv = await db.PharmacyInventories.FirstOrDefaultAsync(pi => pi.BranchId == branch.Id && pi.MedicineId == medicine.Id);

            Assert.Equal(ReservationStatus.Expired, res!.Status);
            Assert.NotNull(res.ExpiredAt);
            Assert.Equal(20, inv!.AvailableQuantity);
            Assert.Equal(0, inv.ReservedQuantity);
        }
    }

    [Fact]
    public async Task CancelReservation_AfterExpiration_Returns409AndTransitionsToExpired()
    {
        // Arrange: 20 available, customer reserves 5 -> 15 available, 5 reserved
        var (_, branch, medicine, _) = await SeedInventoryAsync(availableQuantity: 20);
        var (custToken, _) = CreateTokenAndUser(UserRole.Customer);

        var createReq = new HttpRequestMessage(HttpMethod.Post, "/api/v1/reservations")
        {
            Content = JsonContent.Create(new CreateReservationRequest(branch.Id, medicine.Id, 5))
        };
        createReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", custToken);
        var createResp = await _client.SendAsync(createReq);
        var created = await createResp.Content.ReadFromJsonAsync<ReservationDto>();

        // Simulate expiration
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<PharmacyDbContext>();
            await db.Database.ExecuteSqlRawAsync(
                "UPDATE Reservations SET CreatedAt = {0}, ExpiresAt = {1} WHERE Id = {2}",
                DateTime.UtcNow.AddHours(-3), DateTime.UtcNow.AddHours(-1), created!.Id);
        }

        // Act: Customer attempts to cancel the expired reservation
        var cancelReq = new HttpRequestMessage(HttpMethod.Post, $"/api/v1/reservations/{created!.Id}/cancel");
        cancelReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", custToken);
        var cancelResp = await _client.SendAsync(cancelReq);

        // Assert: 409 Conflict
        Assert.Equal(HttpStatusCode.Conflict, cancelResp.StatusCode);

        // Verify that reservation transitioned to Expired and stock was restored to 20 available
        using (var verifyScope = _factory.Services.CreateScope())
        {
            var db = verifyScope.ServiceProvider.GetRequiredService<PharmacyDbContext>();
            var res = await db.Reservations.FindAsync(created.Id);
            var inv = await db.PharmacyInventories.FirstOrDefaultAsync(pi => pi.BranchId == branch.Id && pi.MedicineId == medicine.Id);

            Assert.Equal(ReservationStatus.Expired, res!.Status);
            Assert.NotNull(res.ExpiredAt);
            Assert.Equal(20, inv!.AvailableQuantity);
            Assert.Equal(0, inv.ReservedQuantity);
        }
    }

    [Fact]
    public async Task ConcurrentCancellationAndConfirmation_OnlyOneWins()
    {
        // Arrange: 20 available, customer reserves 5 -> 15 available, 5 reserved
        var (pharmacy, branch, medicine, _) = await SeedInventoryAsync(availableQuantity: 20);
        var (custToken, _) = CreateTokenAndUser(UserRole.Customer);
        var (staffToken, _) = CreateTokenAndUser(UserRole.PharmacyStaff, pharmacy.Id);

        var createReq = new HttpRequestMessage(HttpMethod.Post, "/api/v1/reservations")
        {
            Content = JsonContent.Create(new CreateReservationRequest(branch.Id, medicine.Id, 5))
        };
        createReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", custToken);
        var createResp = await _client.SendAsync(createReq);
        var created = await createResp.Content.ReadFromJsonAsync<ReservationDto>();

        // Act: Run cancellation and confirmation concurrently
        var client1 = _factory.CreateClient();
        var client2 = _factory.CreateClient();

        var cancelTask = Task.Run(async () =>
        {
            var req = new HttpRequestMessage(HttpMethod.Post, $"/api/v1/reservations/{created!.Id}/cancel");
            req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", custToken);
            return await client1.SendAsync(req);
        });

        var confirmTask = Task.Run(async () =>
        {
            var req = new HttpRequestMessage(HttpMethod.Post, $"/api/v1/reservations/{created!.Id}/confirm");
            req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", staffToken);
            return await client2.SendAsync(req);
        });

        var responses = await Task.WhenAll(cancelTask, confirmTask);

        // Assert: Exactly one 200 OK and one 409 Conflict
        var okCount = responses.Count(r => r.StatusCode == HttpStatusCode.OK);
        var conflictCount = responses.Count(r => r.StatusCode == HttpStatusCode.Conflict);

        Assert.Equal(1, okCount);
        Assert.Equal(1, conflictCount);

        // Verify inventory consistency: either 20/0 (if cancel won) or 15/0 (if confirm won), never corrupted
        using var verifyScope = _factory.Services.CreateScope();
        var db = verifyScope.ServiceProvider.GetRequiredService<PharmacyDbContext>();
        var finalRes = await db.Reservations.FindAsync(created!.Id);
        var finalInv = await db.PharmacyInventories.FirstOrDefaultAsync(pi => pi.BranchId == branch.Id && pi.MedicineId == medicine.Id);

        Assert.True(finalRes!.Status is ReservationStatus.Cancelled or ReservationStatus.Confirmed);
        Assert.Equal(0, finalInv!.ReservedQuantity);
        Assert.True(finalInv.AvailableQuantity is 20 or 15);
    }

    [Fact]
    public async Task ConcurrentCancellationAndExpiration_OnlyOneWins_NoDoubleRelease()
    {
        // Arrange: 20 available, customer reserves 5 -> 15 available, 5 reserved
        var (_, branch, medicine, _) = await SeedInventoryAsync(availableQuantity: 20);
        var (custToken, _) = CreateTokenAndUser(UserRole.Customer);

        var createReq = new HttpRequestMessage(HttpMethod.Post, "/api/v1/reservations")
        {
            Content = JsonContent.Create(new CreateReservationRequest(branch.Id, medicine.Id, 5))
        };
        createReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", custToken);
        var createResp = await _client.SendAsync(createReq);
        var created = await createResp.Content.ReadFromJsonAsync<ReservationDto>();

        // Set to overdue so sweep will consider it
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<PharmacyDbContext>();
            await db.Database.ExecuteSqlRawAsync(
                "UPDATE Reservations SET CreatedAt = {0}, ExpiresAt = {1} WHERE Id = {2}",
                DateTime.UtcNow.AddHours(-3), DateTime.UtcNow.AddHours(-1), created!.Id);
        }

        // Act: Run customer cancel HTTP request and expiration sweep service concurrently
        var cancelTask = Task.Run(async () =>
        {
            var req = new HttpRequestMessage(HttpMethod.Post, $"/api/v1/reservations/{created!.Id}/cancel");
            req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", custToken);
            return await _client.SendAsync(req);
        });

        var sweepTask = Task.Run(async () =>
        {
            using var sweepScope = _factory.Services.CreateScope();
            var service = sweepScope.ServiceProvider.GetRequiredService<IReservationService>();
            return await service.ExpireOverdueReservationsAsync();
        });

        await Task.WhenAll(cancelTask, sweepTask);

        // Verify: Exactly 20 available, 0 reserved! NO double release (which would yield 25)!
        using var verifyScope = _factory.Services.CreateScope();
        var verifyDb = verifyScope.ServiceProvider.GetRequiredService<PharmacyDbContext>();
        var finalRes = await verifyDb.Reservations.FindAsync(created!.Id);
        var finalInv = await verifyDb.PharmacyInventories.FirstOrDefaultAsync(pi => pi.BranchId == branch.Id && pi.MedicineId == medicine.Id);

        Assert.True(finalRes!.Status is ReservationStatus.Cancelled or ReservationStatus.Expired);
        Assert.Equal(20, finalInv!.AvailableQuantity); // Absolutely must be 20, never 25!
        Assert.Equal(0, finalInv.ReservedQuantity);
    }

    [Fact]
    public async Task AdminReleaseReservation_PlatformAdmin_RestoresStockAndSetsReleasedAt()
    {
        // Arrange: 20 available, customer reserves 5 -> 15 available, 5 reserved
        var (_, branch, medicine, _) = await SeedInventoryAsync(availableQuantity: 20);
        var (custToken, _) = CreateTokenAndUser(UserRole.Customer);
        var (adminToken, _) = CreateTokenAndUser(UserRole.PlatformAdmin);

        var createReq = new HttpRequestMessage(HttpMethod.Post, "/api/v1/reservations")
        {
            Content = JsonContent.Create(new CreateReservationRequest(branch.Id, medicine.Id, 5))
        };
        createReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", custToken);
        var createResp = await _client.SendAsync(createReq);
        var created = await createResp.Content.ReadFromJsonAsync<ReservationDto>();

        // Act: PlatformAdmin administratively releases reservation
        var releaseReq = new HttpRequestMessage(HttpMethod.Post, $"/api/v1/reservations/{created!.Id}/release")
        {
            Content = JsonContent.Create(new AdminReleaseReservationRequest("Emergency branch closure"))
        };
        releaseReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);
        var releaseResp = await _client.SendAsync(releaseReq);

        // Assert
        Assert.Equal(HttpStatusCode.OK, releaseResp.StatusCode);
        var released = await releaseResp.Content.ReadFromJsonAsync<ReservationDto>();
        Assert.Equal("Released", released!.Status);
        Assert.NotNull(released.ReleasedAt);

        // Verify inventory restored: 20 available, 0 reserved
        using var verifyScope = _factory.Services.CreateScope();
        var db = verifyScope.ServiceProvider.GetRequiredService<PharmacyDbContext>();
        var finalInv = await db.PharmacyInventories.FirstOrDefaultAsync(pi => pi.BranchId == branch.Id && pi.MedicineId == medicine.Id);

        Assert.Equal(20, finalInv!.AvailableQuantity);
        Assert.Equal(0, finalInv.ReservedQuantity);
    }

    [Fact]
    public async Task AdminReleaseReservation_Customer_Returns403Forbidden()
    {
        var (custToken, _) = CreateTokenAndUser(UserRole.Customer);
        var releaseReq = new HttpRequestMessage(HttpMethod.Post, $"/api/v1/reservations/{Guid.NewGuid()}/release")
        {
            Content = JsonContent.Create(new AdminReleaseReservationRequest("Customer attempt"))
        };
        releaseReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", custToken);
        var releaseResp = await _client.SendAsync(releaseReq);

        Assert.Equal(HttpStatusCode.Forbidden, releaseResp.StatusCode);
    }
}
