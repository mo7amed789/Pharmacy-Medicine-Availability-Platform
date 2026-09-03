using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using PharmacyAvailability.Domain.Catalog;
using PharmacyAvailability.Domain.Identity;
using PharmacyAvailability.Domain.Inventory;
using PharmacyAvailability.Domain.Pharmacies;
using Xunit;

namespace PharmacyAvailability.IntegrationTests;

public class DatabaseConstraintTests : IClassFixture<DatabaseFixture>
{
    private readonly DatabaseFixture _fixture;

    public DatabaseConstraintTests(DatabaseFixture fixture)
    {
        _fixture = fixture;
    }

    private async Task<(Pharmacy pharmacy, PharmacyBranch branch, Medicine medicine, User user)> CreateBaseEntitiesAsync()
    {
        using var context = _fixture.CreateContext();

        var category = new Category($"Cat_{Guid.NewGuid():N}");
        var manufacturer = new Manufacturer($"Man_{Guid.NewGuid():N}");
        context.Categories.Add(category);
        context.Manufacturers.Add(manufacturer);
        await context.SaveChangesAsync();

        var medicine = new Medicine(
            $"Panadol_{Guid.NewGuid():N}",
            "Paracetamol",
            "Paracetamol 500mg",
            "500mg",
            "Tablet",
            category.Id,
            manufacturer.Id);
        context.Medicines.Add(medicine);

        var pharmacy = new Pharmacy(
            $"Pharmacy_{Guid.NewGuid():N}",
            $"LIC_{Guid.NewGuid():N}",
            "10 Nile St",
            "Cairo",
            new Coordinates(30.0444, 31.2357),
            "+201000000001");
        var branch = pharmacy.AddBranch(
            "Main Branch",
            "10 Nile St",
            "Cairo",
            new Coordinates(30.0444, 31.2357),
            "+201000000001");
        context.Pharmacies.Add(pharmacy);

        var user = new User(
            $"user_{Guid.NewGuid():N}@test.com",
            "hashed_pwd",
            "Test Admin",
            UserRole.PlatformAdmin);
        context.Users.Add(user);

        await context.SaveChangesAsync();
        return (pharmacy, branch, medicine, user);
    }

    [Fact]
    public async Task DatabaseConstraint_NegativeInventoryQuantity_RejectedBySqlServer()
    {
        var (pharmacy, branch, medicine, _) = await CreateBaseEntitiesAsync();
        using var context = _fixture.CreateContext();

        // Attempt raw SQL insert with negative quantity to bypass domain logic and test SQL Server check constraint
        var inventoryId = Guid.NewGuid();
        var sql = @"
            INSERT INTO PharmacyInventories (Id, PharmacyId, BranchId, MedicineId, AvailableQuantity, ReservedQuantity, Price, Currency, Source, UpdatedAt, CreatedAt)
            VALUES (@p0, @p1, @p2, @p3, @p4, @p5, @p6, @p7, @p8, SYSUTCDATETIME(), SYSUTCDATETIME())";

        var ex = await Assert.ThrowsAsync<SqlException>(() =>
            context.Database.ExecuteSqlRawAsync(sql, inventoryId, pharmacy.Id, branch.Id, medicine.Id, -5, 0, 50.00m, "EGP", 1));

        Assert.Contains("CK_PharmacyInventories_AvailableQuantity", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task DatabaseConstraint_NegativeInventoryPrice_RejectedBySqlServer()
    {
        var (pharmacy, branch, medicine, _) = await CreateBaseEntitiesAsync();
        using var context = _fixture.CreateContext();

        var inventoryId = Guid.NewGuid();
        var sql = @"
            INSERT INTO PharmacyInventories (Id, PharmacyId, BranchId, MedicineId, AvailableQuantity, ReservedQuantity, Price, Currency, Source, UpdatedAt, CreatedAt)
            VALUES (@p0, @p1, @p2, @p3, @p4, @p5, @p6, @p7, @p8, SYSUTCDATETIME(), SYSUTCDATETIME())";

        var ex = await Assert.ThrowsAsync<SqlException>(() =>
            context.Database.ExecuteSqlRawAsync(sql, inventoryId, pharmacy.Id, branch.Id, medicine.Id, 10, 0, -10.00m, "EGP", 1));

        Assert.Contains("CK_PharmacyInventories_Price", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData(95.0, 31.0, "CK_Pharmacies_Latitude")]
    [InlineData(-95.0, 31.0, "CK_Pharmacies_Latitude")]
    [InlineData(30.0, 190.0, "CK_Pharmacies_Longitude")]
    [InlineData(30.0, -190.0, "CK_Pharmacies_Longitude")]
    public async Task DatabaseConstraint_InvalidCoordinates_RejectedBySqlServer(decimal lat, decimal lon, string expectedConstraint)
    {
        using var context = _fixture.CreateContext();

        var pharmacyId = Guid.NewGuid();
        var sql = @"
            INSERT INTO Pharmacies (Id, Name, LicenseNumber, Address, City, Latitude, Longitude, Phone, Status, UpdatedAt, CreatedAt)
            VALUES (@p0, @p1, @p2, @p3, @p4, @p5, @p6, @p7, @p8, SYSUTCDATETIME(), SYSUTCDATETIME())";

        var ex = await Assert.ThrowsAsync<SqlException>(() =>
            context.Database.ExecuteSqlRawAsync(
                sql,
                pharmacyId,
                "Bad Coordinates Pharmacy",
                $"LIC_{Guid.NewGuid():N}",
                "Address",
                "City",
                lat,
                lon,
                "+201000000000",
                1));

        Assert.Contains(expectedConstraint, ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task DatabaseConstraint_DuplicateUserEmail_RejectedBySqlServer()
    {
        using var context = _fixture.CreateContext();
        var email = $"duplicate_{Guid.NewGuid():N}@test.com";

        var u1 = new User(email, "pwd1", "User One", UserRole.Customer);
        var u2 = new User(email, "pwd2", "User Two", UserRole.Customer);

        context.Users.Add(u1);
        await context.SaveChangesAsync();

        context.Users.Add(u2);
        var ex = await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync());
        Assert.Contains("IX_Users_Email", ex.InnerException?.Message ?? ex.Message);
    }

    [Fact]
    public async Task DatabaseConstraint_DuplicatePharmacyMedicineInventory_RejectedBySqlServer()
    {
        var (pharmacy, branch, medicine, _) = await CreateBaseEntitiesAsync();
        using var context = _fixture.CreateContext();

        var inv1 = new PharmacyInventory(pharmacy.Id, branch.Id, medicine.Id, 10, 50m);
        var inv2 = new PharmacyInventory(pharmacy.Id, branch.Id, medicine.Id, 20, 60m);

        context.PharmacyInventories.Add(inv1);
        await context.SaveChangesAsync();

        context.PharmacyInventories.Add(inv2);
        var ex = await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync());
        Assert.Contains("IX_PharmacyInventories_BranchId_MedicineId", ex.InnerException?.Message ?? ex.Message);
    }

    [Fact]
    public async Task DatabaseConstraint_DuplicateBarcode_RejectedBySqlServer()
    {
        var (_, _, medicine, _) = await CreateBaseEntitiesAsync();
        using var context = _fixture.CreateContext();

        var barcode = $"BAR_{Guid.NewGuid():N}";
        var med1 = await context.Medicines.FindAsync(medicine.Id);
        med1!.AddBarcode(barcode, true);
        await context.SaveChangesAsync();

        // Create a second medicine and add the identical barcode
        var cat = await context.Categories.FirstAsync();
        var man = await context.Manufacturers.FirstAsync();
        var med2 = new Medicine("Med2", "Gen2", "Act2", "10mg", "Capsule", cat.Id, man.Id);
        med2.AddBarcode(barcode, true);
        context.Medicines.Add(med2);

        var ex = await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync());
        Assert.Contains("IX_MedicineBarcodes_Barcode", ex.InnerException?.Message ?? ex.Message);
    }

    [Fact]
    public async Task DeleteBehavior_InventoryWithTransactions_DeleteRestricted()
    {
        var (pharmacy, branch, medicine, user) = await CreateBaseEntitiesAsync();
        using var context = _fixture.CreateContext();

        var inv = new PharmacyInventory(pharmacy.Id, branch.Id, medicine.Id, 10, 50m);
        inv.AdjustStock(-2, InventoryAdjustmentReason.Sale, user.Id, "Test sale");
        context.PharmacyInventories.Add(inv);
        await context.SaveChangesAsync();

        // Attempt raw SQL DELETE on inventory row when audit transaction exists - directly tests SQL Server FK restriction
        using var deleteContext = _fixture.CreateContext();
        var ex = await Assert.ThrowsAsync<SqlException>(() =>
            deleteContext.Database.ExecuteSqlRawAsync("DELETE FROM PharmacyInventories WHERE Id = {0}", inv.Id));
        Assert.Contains("FK_InventoryTransactions_PharmacyInventories_PharmacyInventoryId", ex.Message);
    }
}
