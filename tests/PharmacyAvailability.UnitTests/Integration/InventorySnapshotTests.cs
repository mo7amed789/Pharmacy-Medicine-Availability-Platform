using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using PharmacyAvailability.Api.Integration;
using PharmacyAvailability.Infrastructure.Persistence;
using System.Security.Cryptography;
using System.Text;

namespace PharmacyAvailability.UnitTests.Integration;

public class InventorySnapshotTests
{
    private static InventorySnapshot Valid() => new(1, Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
        1, DateTime.UtcNow, "partial", [new("1", Guid.NewGuid(), new string('a', 64), 0, 45.25m, "EGP", "box", 20)]);

    [Fact] public void AppDecimalStringsDeserialize()
    {
        var s = Valid();
        var json = System.Text.Json.JsonSerializer.Serialize(s).Replace("45.25", "\"45.25\"");
        Assert.Equal(45.25m, System.Text.Json.JsonSerializer.Deserialize<InventorySnapshot>(json)!.Items[0].Price);
    }
    [Fact] public void ValidZeroStockIsAccepted() => Assert.Null(SnapshotRules.Validate(Valid(), DateTime.UtcNow));
    [Fact] public void DuplicateProductsRejected()
    {
        var s = Valid();
        Assert.NotNull(SnapshotRules.Validate(s with { Items = [s.Items[0], s.Items[0]] }, DateTime.UtcNow));
    }
    [Fact] public void StaleAndFutureObservationsRejected()
    {
        var s = Valid();
        Assert.NotNull(SnapshotRules.Validate(s with { ObservedAtUtc = DateTime.UtcNow.AddMinutes(-16) }, DateTime.UtcNow));
        Assert.NotNull(SnapshotRules.Validate(s with { ObservedAtUtc = DateTime.UtcNow.AddMinutes(3) }, DateTime.UtcNow));
    }
    [Fact] public void PricesUnitsAndEnvelopeValidated()
    {
        var s = Valid();
        foreach (var item in new[] { s.Items[0] with { Price = 0 }, s.Items[0] with { Price = 1.111m },
            s.Items[0] with { AvailableQuantity = -1 }, s.Items[0] with { SaleUnit = "pill" },
            s.Items[0] with { UnitsPerPack = 0 }, s.Items[0] with { Currency = "USD" },
            s.Items[0] with { IdentityFingerprint = "unreviewed" } })
            Assert.NotNull(SnapshotRules.Validate(s with { Items = [item] }, DateTime.UtcNow));
        Assert.NotNull(SnapshotRules.Validate(s with { Mode = "full" }, DateTime.UtcNow));
        Assert.NotNull(SnapshotRules.Validate(s with { Items = [] }, DateTime.UtcNow));
    }
    [Fact] public void CredentialFailsClosed()
    {
        var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes("synthetic-test-key")));
        Assert.True(SnapshotRules.KeyMatches("synthetic-test-key", hash));
        Assert.False(SnapshotRules.KeyMatches("wrong", hash));
        Assert.False(SnapshotRules.KeyMatches(null, hash));
        Assert.False(SnapshotRules.KeyMatches("x", new string('z',64)));
    }
    [Fact] public async Task MissingCredentialRejectedBeforeDatabaseAccess()
    {
        using var db = new PharmacyDbContext(new DbContextOptionsBuilder<PharmacyDbContext>().UseSqlServer("Server=unused;Database=never_connected;Integrated Security=true").Options);
        var c = new InventorySnapshotsController(db, new ConfigurationBuilder().Build()) {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() } };
        c.Request.Scheme = "https";
        Assert.IsType<UnauthorizedResult>(await c.Receive(Valid(), default));
    }
    [Fact] public async Task CrossBranchKeyRejectedBeforeDatabaseAccess()
    {
        var s = Valid();
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string,string?> {
            ["InventoryIntegration:Publishers:0:PharmacyId"] = Guid.NewGuid().ToString(),
            ["InventoryIntegration:Publishers:0:BranchId"] = Guid.NewGuid().ToString(),
            ["InventoryIntegration:Publishers:0:InstallationId"] = s.InstallationId.ToString(),
            ["InventoryIntegration:Publishers:0:KeySha256"] = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(new string('k',32)))) }).Build();
        using var db = new PharmacyDbContext(new DbContextOptionsBuilder<PharmacyDbContext>().UseSqlServer("Server=unused;Database=never_connected;Integrated Security=true").Options);
        var c = new InventorySnapshotsController(db, config) {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() } };
        c.Request.Scheme = "https";
        c.Request.Headers["X-Inventory-Publisher-Key"] = new string('k',32);
        Assert.Equal(403, Assert.IsType<StatusCodeResult>(await c.Receive(s, default)).StatusCode);
    }
}
