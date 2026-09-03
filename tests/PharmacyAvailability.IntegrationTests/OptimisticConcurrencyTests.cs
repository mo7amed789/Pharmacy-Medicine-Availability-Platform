using Microsoft.EntityFrameworkCore;
using PharmacyAvailability.Domain.Catalog;
using PharmacyAvailability.Domain.Identity;
using PharmacyAvailability.Domain.Inventory;
using PharmacyAvailability.Domain.Pharmacies;
using Xunit;
using Xunit.Abstractions;
using PharmacyAvailability.Infrastructure.Persistence;

namespace PharmacyAvailability.IntegrationTests;

public class OptimisticConcurrencyTests : IClassFixture<DatabaseFixture>
{
    private readonly DatabaseFixture _fixture;
    private readonly ITestOutputHelper _output;

    public OptimisticConcurrencyTests(DatabaseFixture fixture, ITestOutputHelper output)
    {
        _fixture = fixture;
        _output = output;
    }

    [Fact]
    public async Task OptimisticConcurrency_SimultaneousUpdates_TriggersDbUpdateConcurrencyException()
    {
        // 1. Arrange: Create seed records
        using var setupContext = _fixture.CreateContext();
        var cat = new Category($"Cat_{Guid.NewGuid():N}");
        var man = new Manufacturer($"Man_{Guid.NewGuid():N}");
        setupContext.Categories.Add(cat);
        setupContext.Manufacturers.Add(man);
        await setupContext.SaveChangesAsync();

        var med = new Medicine($"Med_{Guid.NewGuid():N}", "Gen", "Act", "100mg", "Tablet", cat.Id, man.Id);
        var pharmacy = new Pharmacy($"Pharm_{Guid.NewGuid():N}", $"LIC_{Guid.NewGuid():N}", "Street", "City", new Coordinates(30.0, 31.0), "+201000000000");
        var user = new User($"user_{Guid.NewGuid():N}@test.com", "hash", "Staff", UserRole.PlatformAdmin);

        setupContext.Medicines.Add(med);
        setupContext.Pharmacies.Add(pharmacy);
        setupContext.Users.Add(user);
        await setupContext.SaveChangesAsync();

        var inventory = new PharmacyInventory(pharmacy.Id, med.Id, 20, 100.00m);
        setupContext.PharmacyInventories.Add(inventory);
        await setupContext.SaveChangesAsync();

        var inventoryId = inventory.Id;

        // 2. Act: Load the same inventory record in two independent DbContexts
        var optionsA = new DbContextOptionsBuilder<PharmacyDbContext>()
            .UseSqlServer(_fixture.ConnectionString)
            .LogTo(_output.WriteLine, Microsoft.Extensions.Logging.LogLevel.Information)
            .EnableSensitiveDataLogging()
            .Options;
        using var contextUserA = new PharmacyDbContext(optionsA);
        using var contextUserB = _fixture.CreateContext();

        var inventoryA = await contextUserA.PharmacyInventories.FindAsync(inventoryId);
        var inventoryB = await contextUserB.PharmacyInventories.FindAsync(inventoryId);

        Assert.NotNull(inventoryA);
        Assert.NotNull(inventoryB);

        _output.WriteLine($"inventoryA RowVersion: {Convert.ToHexString(inventoryA.RowVersion)}");
        _output.WriteLine($"inventoryB RowVersion: {Convert.ToHexString(inventoryB.RowVersion)}");

        try
        {
            // User A modifies stock first and commits
            inventoryA.AdjustStock(-5, InventoryAdjustmentReason.Sale, user.Id, "User A sale");
            await contextUserA.SaveChangesAsync();
            _output.WriteLine($"User A save succeeded! New RowVersion: {Convert.ToHexString(inventoryA.RowVersion)}");
        }
        catch (Exception saveEx)
        {
            _output.WriteLine($"User A save failed with: {saveEx}");
            throw;
        }

        // User B attempts to adjust stock using stale RowVersion
        inventoryB.AdjustStock(-3, InventoryAdjustmentReason.Sale, user.Id, "User B concurrent sale");

        // 3. Assert: User B's save must be rejected by EF Core / SQL Server with DbUpdateConcurrencyException
        var ex = await Assert.ThrowsAsync<DbUpdateConcurrencyException>(() => contextUserB.SaveChangesAsync());
        Assert.NotNull(ex);

        // Verify database state: only User A's update went through
        using var verifyContext = _fixture.CreateContext();
        var currentInventory = await verifyContext.PharmacyInventories.FindAsync(inventoryId);
        Assert.NotNull(currentInventory);
        Assert.Equal(15, currentInventory.AvailableQuantity);
    }
}
