using Microsoft.EntityFrameworkCore;
using PharmacyAvailability.Domain.Catalog;
using PharmacyAvailability.Domain.Inventory;
using PharmacyAvailability.Domain.Pharmacies;
using Xunit;

namespace PharmacyAvailability.IntegrationTests;

public class PrecisionAndUtcTests : IClassFixture<DatabaseFixture>
{
    private readonly DatabaseFixture _fixture;

    public PrecisionAndUtcTests(DatabaseFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task MoneyPrecision_Decimal18_2_PreservesExactCentsWithoutRoundingErrors()
    {
        using var context = _fixture.CreateContext();
        var cat = new Category($"Cat_{Guid.NewGuid():N}");
        var man = new Manufacturer($"Man_{Guid.NewGuid():N}");
        context.Categories.Add(cat);
        context.Manufacturers.Add(man);
        await context.SaveChangesAsync();

        var med = new Medicine($"Med_{Guid.NewGuid():N}", "Gen", "Act", "10mg", "Tab", cat.Id, man.Id);
        var pharm = new Pharmacy($"Pharm_{Guid.NewGuid():N}", $"LIC_{Guid.NewGuid():N}", "Addr", "City", new Coordinates(30.0, 31.0), "+20100");
        var branch = pharm.AddBranch("Branch", "Addr", "City", new Coordinates(30.0, 31.0), "+20100");
        context.Medicines.Add(med);
        context.Pharmacies.Add(pharm);
        await context.SaveChangesAsync();

        var exactPrice = 149.99m;
        var inventory = new PharmacyInventory(pharm.Id, branch.Id, med.Id, 10, exactPrice);
        context.PharmacyInventories.Add(inventory);
        await context.SaveChangesAsync();

        // Read in a separate context
        using var readContext = _fixture.CreateContext();
        var saved = await readContext.PharmacyInventories.FindAsync(inventory.Id);
        Assert.NotNull(saved);
        Assert.Equal(149.99m, saved.Price);
    }

    [Fact]
    public async Task DateTime_StorageAndRetrieval_MaintainsUtcKind()
    {
        using var context = _fixture.CreateContext();
        var cat = new Category($"Cat_{Guid.NewGuid():N}");
        context.Categories.Add(cat);
        await context.SaveChangesAsync();

        using var readContext = _fixture.CreateContext();
        var saved = await readContext.Categories.FindAsync(cat.Id);
        Assert.NotNull(saved);
        Assert.Equal(DateTimeKind.Utc, saved.CreatedAt.Kind);
    }
}
