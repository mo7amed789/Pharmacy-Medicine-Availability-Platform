using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using PharmacyAvailability.Application.Common.Interfaces;
using PharmacyAvailability.Contracts.Catalog;
using PharmacyAvailability.Domain.Catalog;
using PharmacyAvailability.Domain.Identity;
using PharmacyAvailability.Infrastructure.Persistence;
using Xunit;

namespace PharmacyAvailability.IntegrationTests;

public class CatalogIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public CatalogIntegrationTests(WebApplicationFactory<Program> factory)
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
        var user = new User($"user_{Guid.NewGuid():N}@test.com", "hash", "Test User", role, "+2010", pharmacyId);
        var (token, _) = tokenService.GenerateAccessToken(user);
        return token;
    }

    private async Task<(Guid CategoryId, Guid ManufacturerId)> CreateBaseCatalogEntitiesAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PharmacyDbContext>();

        var category = new Category($"Analgesics {Guid.NewGuid():N}", "Pain relievers");
        var manufacturer = new Manufacturer($"PharmaCorp {Guid.NewGuid():N}", "Egypt");

        db.Categories.Add(category);
        db.Manufacturers.Add(manufacturer);
        await db.SaveChangesAsync();

        return (category.Id, manufacturer.Id);
    }

    [Fact]
    public async Task CreateMedicine_AsPlatformAdmin_Returns201Created()
    {
        // Arrange
        var (categoryId, manufacturerId) = await CreateBaseCatalogEntitiesAsync();
        var adminToken = CreateToken(UserRole.PlatformAdmin);

        var barcode = $"BAR_{Guid.NewGuid():N}";
        var request = new CreateMedicineRequest(
            BrandName: $"Panadol Extra {Guid.NewGuid():N}",
            GenericName: "Paracetamol + Caffeine",
            ActiveIngredient: "Paracetamol",
            Strength: "500mg / 65mg",
            DosageForm: "Tablet",
            CategoryId: categoryId,
            ManufacturerId: manufacturerId,
            RequiresPrescription: false,
            InitialBarcode: barcode);

        var httpRequest = new HttpRequestMessage(HttpMethod.Post, "/api/v1/admin/medicines")
        {
            Content = JsonContent.Create(request)
        };
        httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);

        // Act
        var response = await _client.SendAsync(httpRequest);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var medicine = await response.Content.ReadFromJsonAsync<MedicineDto>();
        Assert.NotNull(medicine);
        Assert.Equal(request.BrandName, medicine.BrandName);
        Assert.Single(medicine.Barcodes);
        Assert.Equal(barcode.ToUpperInvariant(), medicine.Barcodes[0].Barcode);
    }

    [Fact]
    public async Task CreateMedicine_AsCustomer_Returns403Forbidden()
    {
        // Arrange
        var (categoryId, manufacturerId) = await CreateBaseCatalogEntitiesAsync();
        var customerToken = CreateToken(UserRole.Customer);

        var request = new CreateMedicineRequest(
            BrandName: "Unauthorized Med",
            GenericName: "Generic",
            ActiveIngredient: "Active",
            Strength: "10mg",
            DosageForm: "Capsule",
            CategoryId: categoryId,
            ManufacturerId: manufacturerId,
            RequiresPrescription: false);

        var httpRequest = new HttpRequestMessage(HttpMethod.Post, "/api/v1/admin/medicines")
        {
            Content = JsonContent.Create(request)
        };
        httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", customerToken);

        // Act
        var response = await _client.SendAsync(httpRequest);

        // Assert: Customer MUST be rejected with 403 Forbidden
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task CreateMedicine_AsPharmacyStaff_Returns403Forbidden()
    {
        // Arrange
        var (categoryId, manufacturerId) = await CreateBaseCatalogEntitiesAsync();
        var staffToken = CreateToken(UserRole.PharmacyStaff, Guid.NewGuid());

        var request = new CreateMedicineRequest(
            BrandName: "Unauthorized Med",
            GenericName: "Generic",
            ActiveIngredient: "Active",
            Strength: "10mg",
            DosageForm: "Capsule",
            CategoryId: categoryId,
            ManufacturerId: manufacturerId,
            RequiresPrescription: false);

        var httpRequest = new HttpRequestMessage(HttpMethod.Post, "/api/v1/admin/medicines")
        {
            Content = JsonContent.Create(request)
        };
        httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", staffToken);

        // Act
        var response = await _client.SendAsync(httpRequest);

        // Assert: Pharmacy staff cannot modify global medicine catalog
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task CreateMedicine_Unauthenticated_Returns401Unauthorized()
    {
        // Arrange
        var (categoryId, manufacturerId) = await CreateBaseCatalogEntitiesAsync();
        var request = new CreateMedicineRequest("NoAuth", "Gen", "Act", "10mg", "Tab", categoryId, manufacturerId, false);

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/admin/medicines", request);

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task SearchMedicines_FiltersByBrandName_AndPaginates()
    {
        // Arrange: Seed two medicines with unique prefix
        var (categoryId, manufacturerId) = await CreateBaseCatalogEntitiesAsync();
        var adminToken = CreateToken(UserRole.PlatformAdmin);
        var uniquePrefix = $"SearchTest_{Guid.NewGuid():N}";

        for (int i = 1; i <= 3; i++)
        {
            var req = new CreateMedicineRequest(
                BrandName: $"{uniquePrefix}_Brand_{i}",
                GenericName: $"Generic_{uniquePrefix}",
                ActiveIngredient: "Amoxicillin",
                Strength: "500mg",
                DosageForm: "Capsule",
                CategoryId: categoryId,
                ManufacturerId: manufacturerId,
                RequiresPrescription: true);

            var postReq = new HttpRequestMessage(HttpMethod.Post, "/api/v1/admin/medicines")
            {
                Content = JsonContent.Create(req)
            };
            postReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);
            var res = await _client.SendAsync(postReq);
            Assert.Equal(HttpStatusCode.Created, res.StatusCode);
        }

        // Act: Search with page=1, pageSize=2
        var searchUrl = $"/api/v1/medicines?searchTerm={uniquePrefix}&page=1&pageSize=2&sortBy=brandName&sortOrder=asc";
        var response = await _client.GetAsync(searchUrl);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var page = await response.Content.ReadFromJsonAsync<PagedResult<MedicineDto>>();
        Assert.NotNull(page);
        Assert.Equal(3, page.TotalCount);
        Assert.Equal(2, page.Items.Count);
        Assert.Equal(1, page.Page);
        Assert.Equal(2, page.TotalPages);
        Assert.True(page.HasNextPage);
        Assert.False(page.HasPreviousPage);

        // Verify deterministic ascending alphabetical sorting
        Assert.Equal($"{uniquePrefix}_Brand_1", page.Items[0].BrandName);
        Assert.Equal($"{uniquePrefix}_Brand_2", page.Items[1].BrandName);
    }

    [Fact]
    public async Task SearchMedicines_FiltersByGenericNameAndCategory()
    {
        // Arrange
        var (categoryId, manufacturerId) = await CreateBaseCatalogEntitiesAsync();
        var adminToken = CreateToken(UserRole.PlatformAdmin);
        var uniqueGeneric = $"UniqueGen_{Guid.NewGuid():N}";

        var req = new CreateMedicineRequest(
            BrandName: "BrandX",
            GenericName: uniqueGeneric,
            ActiveIngredient: "ActiveIngredientX",
            Strength: "100mg",
            DosageForm: "Syrup",
            CategoryId: categoryId,
            ManufacturerId: manufacturerId,
            RequiresPrescription: false);

        var postReq = new HttpRequestMessage(HttpMethod.Post, "/api/v1/admin/medicines")
        {
            Content = JsonContent.Create(req)
        };
        postReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);
        await _client.SendAsync(postReq);

        // Act: Search with generic name and category filter
        var searchUrl = $"/api/v1/medicines?searchTerm={uniqueGeneric}&categoryId={categoryId}";
        var response = await _client.GetAsync(searchUrl);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<PagedResult<MedicineDto>>();
        Assert.NotNull(result);
        Assert.Single(result.Items);
        Assert.Equal(uniqueGeneric, result.Items[0].GenericName);
        Assert.Equal(categoryId, result.Items[0].CategoryId);
    }

    [Fact]
    public async Task GetById_ExistingMedicine_Returns200WithFullDetails()
    {
        // Arrange
        var (categoryId, manufacturerId) = await CreateBaseCatalogEntitiesAsync();
        var adminToken = CreateToken(UserRole.PlatformAdmin);

        var createReq = new CreateMedicineRequest("Cataflam 50mg", "Diclofenac Potassium", "Diclofenac", "50mg", "Tablet", categoryId, manufacturerId, false);
        var postReq = new HttpRequestMessage(HttpMethod.Post, "/api/v1/admin/medicines") { Content = JsonContent.Create(createReq) };
        postReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);
        var postResp = await _client.SendAsync(postReq);
        var created = await postResp.Content.ReadFromJsonAsync<MedicineDto>();

        // Act
        var response = await _client.GetAsync($"/api/v1/medicines/{created!.Id}");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var medicine = await response.Content.ReadFromJsonAsync<MedicineDto>();
        Assert.NotNull(medicine);
        Assert.Equal(created.Id, medicine.Id);
        Assert.Equal("Cataflam 50mg", medicine.BrandName);
        Assert.False(string.IsNullOrWhiteSpace(medicine.CategoryName));
        Assert.False(string.IsNullOrWhiteSpace(medicine.ManufacturerName));
    }

    [Fact]
    public async Task GetById_NonExistentId_Returns404ProblemDetails()
    {
        // Act
        var response = await _client.GetAsync($"/api/v1/medicines/{Guid.NewGuid()}");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task LookupByBarcode_ReturnsMatchingMedicine()
    {
        // Arrange
        var (categoryId, manufacturerId) = await CreateBaseCatalogEntitiesAsync();
        var adminToken = CreateToken(UserRole.PlatformAdmin);
        var barcode = $"622_{Guid.NewGuid():N}";

        var createReq = new CreateMedicineRequest("Augmentin 1g", "Amoxicillin + Clavulanic Acid", "Amoxicillin", "1g", "Tablet", categoryId, manufacturerId, true, InitialBarcode: barcode);
        var postReq = new HttpRequestMessage(HttpMethod.Post, "/api/v1/admin/medicines") { Content = JsonContent.Create(createReq) };
        postReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);
        await _client.SendAsync(postReq);

        // Act: Lookup barcode (case-insensitive test)
        var response = await _client.GetAsync($"/api/v1/medicines/barcode/{barcode.ToLowerInvariant()}");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var medicine = await response.Content.ReadFromJsonAsync<MedicineDto>();
        Assert.NotNull(medicine);
        Assert.Equal("Augmentin 1g", medicine.BrandName);
    }

    [Fact]
    public async Task LookupByBarcode_NonExistentBarcode_Returns404()
    {
        // Act
        var response = await _client.GetAsync($"/api/v1/medicines/barcode/UNKNOWN_BARCODE_{Guid.NewGuid():N}");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task AddBarcode_DuplicateBarcode_RejectedWith400ProblemDetails()
    {
        // Arrange
        var (categoryId, manufacturerId) = await CreateBaseCatalogEntitiesAsync();
        var adminToken = CreateToken(UserRole.PlatformAdmin);
        var sharedBarcode = $"DUP_{Guid.NewGuid():N}";

        var req1 = new CreateMedicineRequest("Med 1", "Gen 1", "Act 1", "10mg", "Tab", categoryId, manufacturerId, false, sharedBarcode);
        var post1 = new HttpRequestMessage(HttpMethod.Post, "/api/v1/admin/medicines") { Content = JsonContent.Create(req1) };
        post1.Headers.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);
        await _client.SendAsync(post1);

        var req2 = new CreateMedicineRequest("Med 2", "Gen 2", "Act 2", "20mg", "Tab", categoryId, manufacturerId, false);
        var post2 = new HttpRequestMessage(HttpMethod.Post, "/api/v1/admin/medicines") { Content = JsonContent.Create(req2) };
        post2.Headers.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);
        var resp2 = await _client.SendAsync(post2);
        var med2 = await resp2.Content.ReadFromJsonAsync<MedicineDto>();

        // Act: Attempt to add the duplicate barcode to Med 2
        var addBarcodeReq = new HttpRequestMessage(HttpMethod.Post, $"/api/v1/admin/medicines/{med2!.Id}/barcodes")
        {
            Content = JsonContent.Create(new AddBarcodeRequest(sharedBarcode, false))
        };
        addBarcodeReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);
        var dupResponse = await _client.SendAsync(addBarcodeReq);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, dupResponse.StatusCode);
        var content = await dupResponse.Content.ReadAsStringAsync();
        Assert.Contains("already in use", content);
    }

    [Fact]
    public async Task AddAlias_AndLookupByAlias_ReturnsMatchingMedicine()
    {
        // Arrange
        var (categoryId, manufacturerId) = await CreateBaseCatalogEntitiesAsync();
        var adminToken = CreateToken(UserRole.PlatformAdmin);
        var alias = $"Congestal_{Guid.NewGuid():N}";

        var createReq = new CreateMedicineRequest("Congestal Tablets", "Paracetamol + Pseudoephedrine", "Paracetamol", "500mg", "Tablet", categoryId, manufacturerId, false);
        var postReq = new HttpRequestMessage(HttpMethod.Post, "/api/v1/admin/medicines") { Content = JsonContent.Create(createReq) };
        postReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);
        var postResp = await _client.SendAsync(postReq);
        var medicine = await postResp.Content.ReadFromJsonAsync<MedicineDto>();

        // Add alias
        var aliasReq = new HttpRequestMessage(HttpMethod.Post, $"/api/v1/admin/medicines/{medicine!.Id}/aliases")
        {
            Content = JsonContent.Create(new AddAliasRequest(alias))
        };
        aliasReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);
        var aliasResp = await _client.SendAsync(aliasReq);
        Assert.Equal(HttpStatusCode.OK, aliasResp.StatusCode);

        // Act: Lookup by alias
        var lookupResp = await _client.GetAsync($"/api/v1/medicines/alias/{alias}");

        // Assert
        Assert.Equal(HttpStatusCode.OK, lookupResp.StatusCode);
        var results = await lookupResp.Content.ReadFromJsonAsync<List<MedicineDto>>();
        Assert.NotNull(results);
        Assert.Single(results);
        Assert.Equal(medicine.Id, results[0].Id);
    }

    [Fact]
    public async Task AddAlias_DuplicateAliasOnSameMedicine_RejectedWith400()
    {
        // Arrange
        var (categoryId, manufacturerId) = await CreateBaseCatalogEntitiesAsync();
        var adminToken = CreateToken(UserRole.PlatformAdmin);
        var alias = $"Flumox_{Guid.NewGuid():N}";

        var createReq = new CreateMedicineRequest("Flumox 500", "Amoxicillin + Flucloxacillin", "Amoxicillin", "500mg", "Capsule", categoryId, manufacturerId, false);
        var postReq = new HttpRequestMessage(HttpMethod.Post, "/api/v1/admin/medicines") { Content = JsonContent.Create(createReq) };
        postReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);
        var postResp = await _client.SendAsync(postReq);
        var medicine = await postResp.Content.ReadFromJsonAsync<MedicineDto>();

        // Add alias first time
        var aliasReq1 = new HttpRequestMessage(HttpMethod.Post, $"/api/v1/admin/medicines/{medicine!.Id}/aliases")
        {
            Content = JsonContent.Create(new AddAliasRequest(alias))
        };
        aliasReq1.Headers.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);
        await _client.SendAsync(aliasReq1);

        // Act: Add duplicate alias
        var aliasReq2 = new HttpRequestMessage(HttpMethod.Post, $"/api/v1/admin/medicines/{medicine.Id}/aliases")
        {
            Content = JsonContent.Create(new AddAliasRequest(alias))
        };
        aliasReq2.Headers.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);
        var dupResp = await _client.SendAsync(aliasReq2);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, dupResp.StatusCode);
    }

    [Fact]
    public async Task UpdateMedicineStatus_DeactivatedMedicine_ExcludedFromDefaultSearch()
    {
        // Arrange
        var (categoryId, manufacturerId) = await CreateBaseCatalogEntitiesAsync();
        var adminToken = CreateToken(UserRole.PlatformAdmin);
        var brandName = $"Discontinued_{Guid.NewGuid():N}";

        var createReq = new CreateMedicineRequest(brandName, "Generic", "Active", "10mg", "Tab", categoryId, manufacturerId, false);
        var postReq = new HttpRequestMessage(HttpMethod.Post, "/api/v1/admin/medicines") { Content = JsonContent.Create(createReq) };
        postReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);
        var postResp = await _client.SendAsync(postReq);
        var medicine = await postResp.Content.ReadFromJsonAsync<MedicineDto>();

        // Deactivate medicine
        var patchReq = new HttpRequestMessage(HttpMethod.Patch, $"/api/v1/admin/medicines/{medicine!.Id}/status")
        {
            Content = JsonContent.Create(new UpdateMedicineStatusRequest(false))
        };
        patchReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);
        var patchResp = await _client.SendAsync(patchReq);
        Assert.Equal(HttpStatusCode.OK, patchResp.StatusCode);

        // Act 1: Default search (activeOnly = true)
        var defaultSearchResp = await _client.GetAsync($"/api/v1/medicines?searchTerm={brandName}");
        var defaultPage = await defaultSearchResp.Content.ReadFromJsonAsync<PagedResult<MedicineDto>>();

        // Assert 1: Deactivated medicine must NOT appear in default active-only search
        Assert.Empty(defaultPage!.Items);

        // Act 2: Explicit search with activeOnly = false
        var allSearchResp = await _client.GetAsync($"/api/v1/medicines?searchTerm={brandName}&activeOnly=false");
        var allPage = await allSearchResp.Content.ReadFromJsonAsync<PagedResult<MedicineDto>>();

        // Assert 2: Medicine appears when activeOnly is false
        Assert.Single(allPage!.Items);
        Assert.False(allPage.Items[0].IsActive);
    }

    [Fact]
    public async Task CategoriesAndManufacturers_PublicListing_Returns200()
    {
        // Arrange
        var (categoryId, manufacturerId) = await CreateBaseCatalogEntitiesAsync();

        // Act
        var catResp = await _client.GetAsync("/api/v1/categories");
        var manResp = await _client.GetAsync("/api/v1/manufacturers");

        // Assert
        Assert.Equal(HttpStatusCode.OK, catResp.StatusCode);
        Assert.Equal(HttpStatusCode.OK, manResp.StatusCode);

        var categories = await catResp.Content.ReadFromJsonAsync<List<CategoryDto>>();
        var manufacturers = await manResp.Content.ReadFromJsonAsync<List<ManufacturerDto>>();

        Assert.NotNull(categories);
        Assert.NotNull(manufacturers);
        Assert.Contains(categories, c => c.Id == categoryId);
        Assert.Contains(manufacturers, m => m.Id == manufacturerId);
    }
}
