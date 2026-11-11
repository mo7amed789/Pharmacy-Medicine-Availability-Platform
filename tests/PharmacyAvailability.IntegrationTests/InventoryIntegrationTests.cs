using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using PharmacyAvailability.Application.Common.Interfaces;
using PharmacyAvailability.Contracts.Inventory;
using PharmacyAvailability.Domain.Catalog;
using PharmacyAvailability.Domain.Identity;
using PharmacyAvailability.Domain.Pharmacies;
using PharmacyAvailability.Infrastructure.Persistence;
using Xunit;

namespace PharmacyAvailability.IntegrationTests;

public class InventoryIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public InventoryIntegrationTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.UseSetting("FeatureFlags:EnableManagementApis", "true");
            builder.UseSetting("FeatureFlags:EnableReservations", "true");
        });
        _client = _factory.CreateClient();
    }

    private string CreateToken(UserRole role, Guid? pharmacyId = null)
    {
        using var scope = _factory.Services.CreateScope();
        var tokenService = scope.ServiceProvider.GetRequiredService<ITokenService>();
        var db = scope.ServiceProvider.GetRequiredService<PharmacyDbContext>();

        var user = new User($"user_{Guid.NewGuid():N}@test.com", "hash", "Test User", role, "+2010", pharmacyId);
        db.Users.Add(user);
        db.SaveChanges();

        var (token, _) = tokenService.GenerateAccessToken(user);
        return token;
    }

    private async Task<(Pharmacy Pharmacy, PharmacyBranch Branch, Medicine Medicine)> SeedTestDataAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PharmacyDbContext>();

        var cat = new Category($"Cat_{Guid.NewGuid():N}");
        var man = new Manufacturer($"Man_{Guid.NewGuid():N}");
        db.Categories.Add(cat);
        db.Manufacturers.Add(man);
        await db.SaveChangesAsync();

        var med = new Medicine(
            $"Panadol_{Guid.NewGuid():N}",
            "Paracetamol",
            "Paracetamol 500mg",
            "500mg",
            "Tablet",
            cat.Id,
            man.Id);
        db.Medicines.Add(med);

        var pharm = new Pharmacy(
            $"Pharm_{Guid.NewGuid():N}",
            $"LIC_{Guid.NewGuid():N}",
            "10 Nile St",
            "Cairo",
            new Coordinates(30.0444, 31.2357),
            "+20100",
            initialStatus: PharmacyStatus.Active);
        var branch = pharm.AddBranch(
            "Main Branch",
            "10 Nile St",
            "Cairo",
            new Coordinates(30.0444, 31.2357),
            "+20100");

        db.Pharmacies.Add(pharm);
        await db.SaveChangesAsync();

        return (pharm, branch, med);
    }

    [Fact]
    public async Task AddInventoryItem_Success_CreatesRecordAndInitialTransaction()
    {
        // Arrange
        var (pharmacy, branch, medicine) = await SeedTestDataAsync();
        var token = CreateToken(UserRole.PharmacyAdmin, pharmacy.Id);

        var request = new CreateInventoryItemRequest(
            MedicineId: medicine.Id,
            InitialQuantity: 25,
            Price: 45.50m,
            Currency: "EGP");

        var httpRequest = new HttpRequestMessage(
            HttpMethod.Post,
            $"/api/v1/pharmacies/{pharmacy.Id}/branches/{branch.Id}/inventory")
        {
            Content = JsonContent.Create(request)
        };
        httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await _client.SendAsync(httpRequest);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var item = await response.Content.ReadFromJsonAsync<InventoryItemDto>();
        Assert.NotNull(item);
        Assert.Equal(pharmacy.Id, item.PharmacyId);
        Assert.Equal(branch.Id, item.BranchId);
        Assert.Equal(medicine.Id, item.MedicineId);
        Assert.Equal(25, item.AvailableQuantity);
        Assert.Equal(0, item.ReservedQuantity);
        Assert.Equal(25, item.TotalStock);
        Assert.Equal(45.50m, item.Price);

        // Verify transaction history contains the initial restock transaction
        var txReq = new HttpRequestMessage(
            HttpMethod.Get,
            $"/api/v1/pharmacies/{pharmacy.Id}/branches/{branch.Id}/inventory/{medicine.Id}/transactions");
        txReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var txResp = await _client.SendAsync(txReq);
        Assert.Equal(HttpStatusCode.OK, txResp.StatusCode);

        var txs = await txResp.Content.ReadFromJsonAsync<List<InventoryTransactionDto>>();
        Assert.NotNull(txs);
        Assert.Single(txs);
        Assert.Equal(25, txs[0].DeltaQuantity);
        Assert.Equal("Restock", txs[0].Reason);
    }

    [Fact]
    public async Task AddInventoryItem_DuplicateMedicineOnSameBranch_Returns409Conflict()
    {
        // Arrange
        var (pharmacy, branch, medicine) = await SeedTestDataAsync();
        var token = CreateToken(UserRole.PharmacyAdmin, pharmacy.Id);

        var request = new CreateInventoryItemRequest(medicine.Id, 10, 50m);

        // Add first time
        var req1 = new HttpRequestMessage(HttpMethod.Post, $"/api/v1/pharmacies/{pharmacy.Id}/branches/{branch.Id}/inventory")
        {
            Content = JsonContent.Create(request)
        };
        req1.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var resp1 = await _client.SendAsync(req1);
        Assert.Equal(HttpStatusCode.Created, resp1.StatusCode);

        // Act: Add second time for identical branch and medicine
        var req2 = new HttpRequestMessage(HttpMethod.Post, $"/api/v1/pharmacies/{pharmacy.Id}/branches/{branch.Id}/inventory")
        {
            Content = JsonContent.Create(request)
        };
        req2.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var resp2 = await _client.SendAsync(req2);

        // Assert
        Assert.Equal(HttpStatusCode.Conflict, resp2.StatusCode);
    }

    [Fact]
    public async Task AdjustStock_PositiveAndNegative_UpdatesAvailableQuantity()
    {
        // Arrange
        var (pharmacy, branch, medicine) = await SeedTestDataAsync();
        var token = CreateToken(UserRole.PharmacyStaff, pharmacy.Id);

        // Create item with 20 units
        var createReq = new CreateInventoryItemRequest(medicine.Id, 20, 50m);
        var addHttp = new HttpRequestMessage(HttpMethod.Post, $"/api/v1/pharmacies/{pharmacy.Id}/branches/{branch.Id}/inventory")
        {
            Content = JsonContent.Create(createReq)
        };
        addHttp.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        await _client.SendAsync(addHttp);

        // Act 1: Restock +10
        var restockReq = new HttpRequestMessage(
            HttpMethod.Patch,
            $"/api/v1/pharmacies/{pharmacy.Id}/branches/{branch.Id}/inventory/{medicine.Id}/quantity")
        {
            Content = JsonContent.Create(new AdjustStockRequest(10, "Restock", "Supplier delivery"))
        };
        restockReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var restockResp = await _client.SendAsync(restockReq);

        Assert.Equal(HttpStatusCode.OK, restockResp.StatusCode);
        var restockedItem = await restockResp.Content.ReadFromJsonAsync<InventoryItemDto>();
        Assert.Equal(30, restockedItem!.AvailableQuantity);

        // Act 2: Sale -5
        var saleReq = new HttpRequestMessage(
            HttpMethod.Patch,
            $"/api/v1/pharmacies/{pharmacy.Id}/branches/{branch.Id}/inventory/{medicine.Id}/quantity")
        {
            Content = JsonContent.Create(new AdjustStockRequest(-5, "Sale", "Walk-in patient"))
        };
        saleReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var saleResp = await _client.SendAsync(saleReq);

        Assert.Equal(HttpStatusCode.OK, saleResp.StatusCode);
        var saleItem = await saleResp.Content.ReadFromJsonAsync<InventoryItemDto>();
        Assert.Equal(25, saleItem!.AvailableQuantity);
    }

    [Fact]
    public async Task AdjustStock_InsufficientStock_Returns409Conflict()
    {
        // Arrange
        var (pharmacy, branch, medicine) = await SeedTestDataAsync();
        var token = CreateToken(UserRole.PharmacyStaff, pharmacy.Id);

        var createReq = new CreateInventoryItemRequest(medicine.Id, 5, 50m);
        var addHttp = new HttpRequestMessage(HttpMethod.Post, $"/api/v1/pharmacies/{pharmacy.Id}/branches/{branch.Id}/inventory")
        {
            Content = JsonContent.Create(createReq)
        };
        addHttp.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        await _client.SendAsync(addHttp);

        // Act: Attempt to decrement by 10 when only 5 exist
        var adjustReq = new HttpRequestMessage(
            HttpMethod.Patch,
            $"/api/v1/pharmacies/{pharmacy.Id}/branches/{branch.Id}/inventory/{medicine.Id}/quantity")
        {
            Content = JsonContent.Create(new AdjustStockRequest(-10, "Sale"))
        };
        adjustReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var adjustResp = await _client.SendAsync(adjustReq);

        // Assert: Mapped to 409 Conflict
        Assert.Equal(HttpStatusCode.Conflict, adjustResp.StatusCode);
    }

    [Fact]
    public async Task Reservation_FullLifecycle_Reserve_Release_Confirm()
    {
        // Arrange
        var (pharmacy, branch, medicine) = await SeedTestDataAsync();
        var token = CreateToken(UserRole.PharmacyStaff, pharmacy.Id);

        // Initial inventory: 10 units
        var createReq = new CreateInventoryItemRequest(medicine.Id, 10, 50m);
        var addHttp = new HttpRequestMessage(HttpMethod.Post, $"/api/v1/pharmacies/{pharmacy.Id}/branches/{branch.Id}/inventory")
        {
            Content = JsonContent.Create(createReq)
        };
        addHttp.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        await _client.SendAsync(addHttp);

        // Step 1: Reserve 4 units
        var reserveReq = new HttpRequestMessage(
            HttpMethod.Post,
            $"/api/v1/pharmacies/{pharmacy.Id}/branches/{branch.Id}/inventory/{medicine.Id}/reserve")
        {
            Content = JsonContent.Create(new ReservationRequest(4, "Online order #101"))
        };
        reserveReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var reserveResp = await _client.SendAsync(reserveReq);

        Assert.Equal(HttpStatusCode.OK, reserveResp.StatusCode);
        var reservedItem = await reserveResp.Content.ReadFromJsonAsync<InventoryItemDto>();
        Assert.Equal(6, reservedItem!.AvailableQuantity);
        Assert.Equal(4, reservedItem.ReservedQuantity);
        Assert.Equal(10, reservedItem.TotalStock);

        // Step 2: Release 1 unit from reservation
        var releaseReq = new HttpRequestMessage(
            HttpMethod.Post,
            $"/api/v1/pharmacies/{pharmacy.Id}/branches/{branch.Id}/inventory/{medicine.Id}/release")
        {
            Content = JsonContent.Create(new ReservationRequest(1, "Customer cancelled item"))
        };
        releaseReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var releaseResp = await _client.SendAsync(releaseReq);

        Assert.Equal(HttpStatusCode.OK, releaseResp.StatusCode);
        var releasedItem = await releaseResp.Content.ReadFromJsonAsync<InventoryItemDto>();
        Assert.Equal(7, releasedItem!.AvailableQuantity);
        Assert.Equal(3, releasedItem.ReservedQuantity);
        Assert.Equal(10, releasedItem.TotalStock);

        // Step 3: Confirm remaining 3 units (pickup completed)
        var confirmReq = new HttpRequestMessage(
            HttpMethod.Post,
            $"/api/v1/pharmacies/{pharmacy.Id}/branches/{branch.Id}/inventory/{medicine.Id}/confirm")
        {
            Content = JsonContent.Create(new ReservationRequest(3, "Customer picked up"))
        };
        confirmReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var confirmResp = await _client.SendAsync(confirmReq);

        Assert.Equal(HttpStatusCode.OK, confirmResp.StatusCode);
        var confirmedItem = await confirmResp.Content.ReadFromJsonAsync<InventoryItemDto>();
        Assert.Equal(7, confirmedItem!.AvailableQuantity);
        Assert.Equal(0, confirmedItem.ReservedQuantity);
        Assert.Equal(7, confirmedItem.TotalStock);
    }

    [Fact]
    public async Task PriceUpdate_CreatesPriceHistory_NoHistoryIfPriceUnchanged()
    {
        // Arrange
        var (pharmacy, branch, medicine) = await SeedTestDataAsync();
        var token = CreateToken(UserRole.PharmacyAdmin, pharmacy.Id);

        var createReq = new CreateInventoryItemRequest(medicine.Id, 10, 50.00m);
        var addHttp = new HttpRequestMessage(HttpMethod.Post, $"/api/v1/pharmacies/{pharmacy.Id}/branches/{branch.Id}/inventory")
        {
            Content = JsonContent.Create(createReq)
        };
        addHttp.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        await _client.SendAsync(addHttp);

        // Act 1: Update price to 65.00m
        var priceReq1 = new HttpRequestMessage(
            HttpMethod.Patch,
            $"/api/v1/pharmacies/{pharmacy.Id}/branches/{branch.Id}/inventory/{medicine.Id}/price")
        {
            Content = JsonContent.Create(new UpdatePriceRequest(65.00m))
        };
        priceReq1.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var priceResp1 = await _client.SendAsync(priceReq1);
        Assert.Equal(HttpStatusCode.OK, priceResp1.StatusCode);

        // Act 2: Submit same price 65.00m again
        var priceReq2 = new HttpRequestMessage(
            HttpMethod.Patch,
            $"/api/v1/pharmacies/{pharmacy.Id}/branches/{branch.Id}/inventory/{medicine.Id}/price")
        {
            Content = JsonContent.Create(new UpdatePriceRequest(65.00m))
        };
        priceReq2.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        await _client.SendAsync(priceReq2);

        // Assert: Query price history - exactly 1 record should exist
        var histReq = new HttpRequestMessage(
            HttpMethod.Get,
            $"/api/v1/pharmacies/{pharmacy.Id}/branches/{branch.Id}/inventory/{medicine.Id}/price-history");
        histReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var histResp = await _client.SendAsync(histReq);

        var history = await histResp.Content.ReadFromJsonAsync<List<PriceHistoryDto>>();
        Assert.NotNull(history);
        Assert.Single(history);
        Assert.Equal(50.00m, history[0].OldPrice);
        Assert.Equal(65.00m, history[0].NewPrice);
    }

    [Fact]
    public async Task MultiTenancy_PharmacyAdminOfPharmacyA_AccessingPharmacyB_Returns403Forbidden()
    {
        // Arrange
        var (pharmacyA, branchA, medicine) = await SeedTestDataAsync();
        var (pharmacyB, branchB, _) = await SeedTestDataAsync();

        var adminTokenA = CreateToken(UserRole.PharmacyAdmin, pharmacyA.Id);

        // Act: Admin of Pharmacy A tries to access Pharmacy B branch inventory
        var request = new HttpRequestMessage(
            HttpMethod.Get,
            $"/api/v1/pharmacies/{pharmacyB.Id}/branches/{branchB.Id}/inventory");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", adminTokenA);

        var response = await _client.SendAsync(request);

        // Assert
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task MultiTenancy_CustomerAccess_Returns403Forbidden()
    {
        // Arrange
        var (pharmacy, branch, _) = await SeedTestDataAsync();
        var customerToken = CreateToken(UserRole.Customer);

        var request = new HttpRequestMessage(
            HttpMethod.Get,
            $"/api/v1/pharmacies/{pharmacy.Id}/branches/{branch.Id}/inventory");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", customerToken);

        // Act
        var response = await _client.SendAsync(request);

        // Assert
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task OperationalStatus_InactiveBranch_RejectsInventoryModifications()
    {
        // Arrange: Create branch and deactivate it
        var (pharmacy, branch, medicine) = await SeedTestDataAsync();
        var token = CreateToken(UserRole.PharmacyAdmin, pharmacy.Id);

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<PharmacyDbContext>();
            var b = await db.PharmacyBranches.FindAsync(branch.Id);
            b!.Deactivate();
            await db.SaveChangesAsync();
        }

        // Act: Attempt to add inventory to inactive branch
        var request = new HttpRequestMessage(
            HttpMethod.Post,
            $"/api/v1/pharmacies/{pharmacy.Id}/branches/{branch.Id}/inventory")
        {
            Content = JsonContent.Create(new CreateInventoryItemRequest(medicine.Id, 10, 50m))
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _client.SendAsync(request);

        // Assert: DomainValidationException mapped to 400 BadRequest ProblemDetails
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task ConcurrentReservations_SimultaneousRequests_PreventsOverselling()
    {
        // Arrange: Seed stock with exactly 5 available units
        var (pharmacy, branch, medicine) = await SeedTestDataAsync();
        var token = CreateToken(UserRole.PharmacyStaff, pharmacy.Id);

        var createReq = new CreateInventoryItemRequest(medicine.Id, 5, 50m);
        var addHttp = new HttpRequestMessage(HttpMethod.Post, $"/api/v1/pharmacies/{pharmacy.Id}/branches/{branch.Id}/inventory")
        {
            Content = JsonContent.Create(createReq)
        };
        addHttp.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        await _client.SendAsync(addHttp);

        // Prepare two concurrent reservation requests for 4 units each (4 + 4 = 8 > 5)
        var client1 = _factory.CreateClient();
        var client2 = _factory.CreateClient();

        Func<HttpClient, Task<HttpResponseMessage>> makeReserveRequest = async (client) =>
        {
            var req = new HttpRequestMessage(
                HttpMethod.Post,
                $"/api/v1/pharmacies/{pharmacy.Id}/branches/{branch.Id}/inventory/{medicine.Id}/reserve")
            {
                Content = JsonContent.Create(new ReservationRequest(4, "Concurrent hold"))
            };
            req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            return await client.SendAsync(req);
        };

        // Act: Execute concurrently
        var task1 = makeReserveRequest(client1);
        var task2 = makeReserveRequest(client2);
        var responses = await Task.WhenAll(task1, task2);

        // Assert: Exactly one request MUST succeed (200 OK), and the other MUST fail with 409 Conflict
        var successCount = responses.Count(r => r.StatusCode == HttpStatusCode.OK);
        var conflictCount = responses.Count(r => r.StatusCode == HttpStatusCode.Conflict);

        Assert.Equal(1, successCount);
        Assert.Equal(1, conflictCount);

        // Verify database state: 1 remaining available, 4 reserved
        var verifyReq = new HttpRequestMessage(
            HttpMethod.Get,
            $"/api/v1/pharmacies/{pharmacy.Id}/branches/{branch.Id}/inventory/{medicine.Id}");
        verifyReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var verifyResp = await _client.SendAsync(verifyReq);
        var currentItem = await verifyResp.Content.ReadFromJsonAsync<InventoryItemDto>();

        Assert.Equal(1, currentItem!.AvailableQuantity);
        Assert.Equal(4, currentItem.ReservedQuantity);
    }
}
