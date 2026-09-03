using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using PharmacyAvailability.Contracts.Availability;
using PharmacyAvailability.Domain.Catalog;
using PharmacyAvailability.Domain.Identity;
using PharmacyAvailability.Domain.Inventory;
using PharmacyAvailability.Domain.Pharmacies;
using PharmacyAvailability.Infrastructure.Persistence;
using Xunit;

namespace PharmacyAvailability.IntegrationTests;

public class AvailabilityIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public AvailabilityIntegrationTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    private async Task<(Pharmacy Pharmacy, PharmacyBranch Branch, Medicine Medicine, PharmacyInventory Inventory)> SeedAvailabilityScenarioAsync(
        PharmacyStatus pharmacyStatus = PharmacyStatus.Active,
        bool branchIsActive = true,
        bool medicineIsActive = true,
        int availableQuantity = 20,
        double branchLat = 30.0444, // Cairo Tahrir
        double branchLon = 31.2357,
        string brandName = "Congestal",
        string genericName = "Paracetamol+Pseudoephedrine",
        string activeIngredient = "Pseudoephedrine Hydrochloride",
        string? barcode = null,
        string? alias = null)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PharmacyDbContext>();

        var cat = new Category($"Cat_{Guid.NewGuid():N}");
        var man = new Manufacturer($"Man_{Guid.NewGuid():N}");
        db.Categories.Add(cat);
        db.Manufacturers.Add(man);
        await db.SaveChangesAsync();

        var med = new Medicine(
            brandName: $"{brandName}_{Guid.NewGuid():N}",
            genericName: genericName,
            activeIngredient: activeIngredient,
            strength: "500mg",
            dosageForm: "Tablet",
            categoryId: cat.Id,
            manufacturerId: man.Id);

        if (!medicineIsActive)
            med.Deactivate();

        if (!string.IsNullOrWhiteSpace(barcode))
            med.AddBarcode(barcode, true);

        if (!string.IsNullOrWhiteSpace(alias))
            med.AddAlias(alias);

        db.Medicines.Add(med);

        var pharm = new Pharmacy(
            name: $"Pharmacy_{Guid.NewGuid():N}",
            licenseNumber: $"LIC_{Guid.NewGuid():N}",
            address: "10 Nile St",
            city: "Cairo",
            coordinates: new Coordinates(branchLat, branchLon),
            phone: "+201000000000",
            initialStatus: pharmacyStatus);

        var branch = pharm.AddBranch(
            name: "Main Branch",
            address: "10 Nile St",
            city: "Cairo",
            coordinates: new Coordinates(branchLat, branchLon),
            phone: "+201000000000");

        if (!branchIsActive)
            branch.Deactivate();

        db.Pharmacies.Add(pharm);
        await db.SaveChangesAsync();

        var inventory = new PharmacyInventory(
            pharmacyId: pharm.Id,
            branchId: branch.Id,
            medicineId: med.Id,
            initialQuantity: availableQuantity,
            initialPrice: 35.00m,
            currency: "EGP",
            source: InventorySource.Manual);

        db.PharmacyInventories.Add(inventory);
        await db.SaveChangesAsync();

        return (pharm, branch, med, inventory);
    }

    [Fact]
    public async Task Search_AnonymousUser_SucceedsWithoutAuthentication()
    {
        // Act
        var response = await _client.GetAsync("/api/v1/availability?q=NonExistentMedicineName");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<AvailabilitySearchResult>();
        Assert.NotNull(result);
        Assert.Empty(result.Items);
        Assert.Equal(0, result.TotalCount);
    }

    [Fact]
    public async Task Search_ByMedicineId_ReturnsMatchingBranchInventory()
    {
        // Arrange
        var (_, branch, medicine, _) = await SeedAvailabilityScenarioAsync();

        // Act
        var response = await _client.GetAsync($"/api/v1/availability?medicineId={medicine.Id}");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<AvailabilitySearchResult>();
        Assert.NotNull(result);
        Assert.Single(result.Items);
        var item = result.Items[0];
        Assert.Equal(medicine.Id, item.MedicineId);
        Assert.Equal(branch.Id, item.BranchId);
        Assert.Equal(20, item.AvailableQuantity);
        Assert.Equal("InStock", item.StockStatus);
        Assert.Null(item.DistanceKm); // Coordinates were not supplied
    }

    [Fact]
    public async Task Search_ByConvenienceEndpoint_ReturnsMatchingAvailability()
    {
        // Arrange
        var (_, branch, medicine, _) = await SeedAvailabilityScenarioAsync();

        // Act
        var response = await _client.GetAsync($"/api/v1/medicines/{medicine.Id}/availability");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<AvailabilitySearchResult>();
        Assert.NotNull(result);
        Assert.Single(result.Items);
        Assert.Equal(medicine.Id, result.Items[0].MedicineId);
    }

    [Fact]
    public async Task Search_ByBarcode_ReturnsMatchingBranchInventory()
    {
        // Arrange
        var barcode = $"BAR_{Guid.NewGuid():N}";
        var (_, _, medicine, _) = await SeedAvailabilityScenarioAsync(barcode: barcode);

        // Act
        var response = await _client.GetAsync($"/api/v1/availability?barcode={barcode}");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<AvailabilitySearchResult>();
        Assert.NotNull(result);
        Assert.Contains(result.Items, i => i.MedicineId == medicine.Id);
    }

    [Fact]
    public async Task Search_ByAlias_ReturnsMatchingBranchInventory()
    {
        // Arrange
        var alias = $"Alias_{Guid.NewGuid():N}";
        var (_, _, medicine, _) = await SeedAvailabilityScenarioAsync(alias: alias);

        // Act
        var response = await _client.GetAsync($"/api/v1/availability?alias={alias}");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<AvailabilitySearchResult>();
        Assert.NotNull(result);
        Assert.Contains(result.Items, i => i.MedicineId == medicine.Id);
    }

    [Fact]
    public async Task Search_ByFreeTextQuery_BrandOrGenericOrIngredient_ReturnsMatching()
    {
        // Arrange
        var uniqueBrand = $"UniqueBrand_{Guid.NewGuid():N}";
        var (_, _, medicine, _) = await SeedAvailabilityScenarioAsync(brandName: uniqueBrand);

        // Act
        var response = await _client.GetAsync($"/api/v1/availability?q={uniqueBrand}");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<AvailabilitySearchResult>();
        Assert.NotNull(result);
        Assert.Contains(result.Items, i => i.MedicineId == medicine.Id);
    }

    [Fact]
    public async Task Search_SuspendedOrPendingPharmacy_ExcludedFromPublicResults()
    {
        // Arrange: Suspended pharmacy and Pending pharmacy
        var (suspPharm, _, suspMed, _) = await SeedAvailabilityScenarioAsync(pharmacyStatus: PharmacyStatus.Suspended);
        var (pendPharm, _, pendMed, _) = await SeedAvailabilityScenarioAsync(pharmacyStatus: PharmacyStatus.Pending);

        // Act
        var respSusp = await _client.GetAsync($"/api/v1/availability?medicineId={suspMed.Id}");
        var respPend = await _client.GetAsync($"/api/v1/availability?medicineId={pendMed.Id}");

        // Assert
        var resSusp = await respSusp.Content.ReadFromJsonAsync<AvailabilitySearchResult>();
        var resPend = await respPend.Content.ReadFromJsonAsync<AvailabilitySearchResult>();

        Assert.Empty(resSusp!.Items);
        Assert.Empty(resPend!.Items);
    }

    [Fact]
    public async Task Search_InactiveBranch_ExcludedFromPublicResults()
    {
        // Arrange: Inactive branch
        var (_, _, medicine, _) = await SeedAvailabilityScenarioAsync(branchIsActive: false);

        // Act
        var response = await _client.GetAsync($"/api/v1/availability?medicineId={medicine.Id}");

        // Assert
        var result = await response.Content.ReadFromJsonAsync<AvailabilitySearchResult>();
        Assert.Empty(result!.Items);
    }

    [Fact]
    public async Task Search_InactiveMedicine_ExcludedFromPublicResults()
    {
        // Arrange: Inactive medicine
        var (_, _, medicine, _) = await SeedAvailabilityScenarioAsync(medicineIsActive: false);

        // Act
        var response = await _client.GetAsync($"/api/v1/availability?medicineId={medicine.Id}");

        // Assert
        var result = await response.Content.ReadFromJsonAsync<AvailabilitySearchResult>();
        Assert.Empty(result!.Items);
    }

    [Fact]
    public async Task Search_ZeroAvailableStock_ExcludedFromPublicResults()
    {
        // Arrange: Available quantity is zero
        var (_, _, medicine, _) = await SeedAvailabilityScenarioAsync(availableQuantity: 0);

        // Act
        var response = await _client.GetAsync($"/api/v1/availability?medicineId={medicine.Id}");

        // Assert
        var result = await response.Content.ReadFromJsonAsync<AvailabilitySearchResult>();
        Assert.Empty(result!.Items);
    }

    [Fact]
    public async Task Search_WithCoordinates_CalculatesDistanceAndFiltersWithinRadius()
    {
        // User at Tahrir Square (30.0444, 31.2357)
        double userLat = 30.0444;
        double userLon = 31.2357;

        // Branch 1: Near Tahrir (~1 km)
        var (_, _, medNear, _) = await SeedAvailabilityScenarioAsync(
            branchLat: 30.0500, branchLon: 31.2400);

        // Branch 2: Far away in Alexandria (~180 km)
        var (_, _, medFar, _) = await SeedAvailabilityScenarioAsync(
            branchLat: 31.2001, branchLon: 29.9187);

        // Act 1: Search within 10km radius for Near branch
        var respNear = await _client.GetAsync(
            $"/api/v1/availability?medicineId={medNear.Id}&latitude={userLat}&longitude={userLon}&radiusKm=10");
        var resNear = await respNear.Content.ReadFromJsonAsync<AvailabilitySearchResult>();

        Assert.Single(resNear!.Items);
        Assert.NotNull(resNear.Items[0].DistanceKm);
        Assert.True(resNear.Items[0].DistanceKm <= 10.0);

        // Act 2: Search within 10km radius for Far branch in Alexandria (should be excluded)
        var respFar = await _client.GetAsync(
            $"/api/v1/availability?medicineId={medFar.Id}&latitude={userLat}&longitude={userLon}&radiusKm=10");
        var resFar = await respFar.Content.ReadFromJsonAsync<AvailabilitySearchResult>();

        Assert.Empty(resFar!.Items);
    }

    [Fact]
    public async Task Search_SortingByDistance_OrdersNearestFirst()
    {
        double userLat = 30.0444;
        double userLon = 31.2357;

        // Same medicine at two different branches
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PharmacyDbContext>();

        var cat = new Category($"Cat_{Guid.NewGuid():N}");
        var man = new Manufacturer($"Man_{Guid.NewGuid():N}");
        db.Categories.Add(cat);
        db.Manufacturers.Add(man);
        await db.SaveChangesAsync();

        var med = new Medicine($"Med_{Guid.NewGuid():N}", "Gen", "Act", "10mg", "Tab", cat.Id, man.Id);
        db.Medicines.Add(med);

        var pharm = new Pharmacy($"Pharm_{Guid.NewGuid():N}", $"LIC_{Guid.NewGuid():N}", "Addr", "Cairo", new Coordinates(30.0, 31.0), "+2010", initialStatus: PharmacyStatus.Active);
        var nearBranch = pharm.AddBranch("Near Branch", "Addr", "Cairo", new Coordinates(30.0500, 31.2400), "+2010"); // ~1 km
        var farBranch = pharm.AddBranch("Far Branch", "Addr", "Cairo", new Coordinates(30.1200, 31.3200), "+2010"); // ~12 km
        db.Pharmacies.Add(pharm);
        await db.SaveChangesAsync();

        var invNear = new PharmacyInventory(pharm.Id, nearBranch.Id, med.Id, 10, 50m);
        var invFar = new PharmacyInventory(pharm.Id, farBranch.Id, med.Id, 10, 50m);
        db.PharmacyInventories.AddRange(invNear, invFar);
        await db.SaveChangesAsync();

        // Act
        var response = await _client.GetAsync(
            $"/api/v1/availability?medicineId={med.Id}&latitude={userLat}&longitude={userLon}&radiusKm=20&sortBy=distance");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<AvailabilitySearchResult>();
        Assert.NotNull(result);
        Assert.Equal(2, result.Items.Count);
        Assert.Equal(nearBranch.Id, result.Items[0].BranchId);
        Assert.Equal(farBranch.Id, result.Items[1].BranchId);
        Assert.True(result.Items[0].DistanceKm < result.Items[1].DistanceKm);
    }

    [Fact]
    public async Task Search_InvalidCoordinatesOrRadius_Returns400BadRequest()
    {
        // Act 1: Latitude only without Longitude
        var resp1 = await _client.GetAsync("/api/v1/availability?latitude=30.0");
        Assert.Equal(HttpStatusCode.BadRequest, resp1.StatusCode);

        // Act 2: Latitude out of range (> 90)
        var resp2 = await _client.GetAsync("/api/v1/availability?latitude=95.0&longitude=31.0");
        Assert.Equal(HttpStatusCode.BadRequest, resp2.StatusCode);

        // Act 3: Longitude out of range (> 180)
        var resp3 = await _client.GetAsync("/api/v1/availability?latitude=30.0&longitude=190.0");
        Assert.Equal(HttpStatusCode.BadRequest, resp3.StatusCode);

        // Act 4: Radius zero or negative
        var resp4 = await _client.GetAsync("/api/v1/availability?latitude=30.0&longitude=31.0&radiusKm=0");
        Assert.Equal(HttpStatusCode.BadRequest, resp4.StatusCode);

        // Act 5: Radius exceeding maximum 100km
        var resp5 = await _client.GetAsync("/api/v1/availability?latitude=30.0&longitude=31.0&radiusKm=150");
        Assert.Equal(HttpStatusCode.BadRequest, resp5.StatusCode);
    }

    [Fact]
    public async Task Search_Pagination_LimitsPageSizeAndProvidesMetadata()
    {
        // Arrange: Seed multiple items
        var (_, _, med, _) = await SeedAvailabilityScenarioAsync();

        // Act
        var response = await _client.GetAsync($"/api/v1/availability?medicineId={med.Id}&page=1&pageSize=1");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<AvailabilitySearchResult>();
        Assert.NotNull(result);
        Assert.Single(result.Items);
        Assert.Equal(1, result.Page);
        Assert.Equal(1, result.PageSize);
        Assert.Equal(1, result.TotalCount);
        Assert.False(result.HasPreviousPage);
        Assert.False(result.HasNextPage);
    }
}
