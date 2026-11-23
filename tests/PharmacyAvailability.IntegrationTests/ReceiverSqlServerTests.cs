using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using PharmacyAvailability.Api.Integration;
using PharmacyAvailability.Contracts.Availability;
using PharmacyAvailability.Domain.Catalog;
using PharmacyAvailability.Domain.Integration;
using PharmacyAvailability.Domain.Inventory;
using PharmacyAvailability.Domain.Pharmacies;
using PharmacyAvailability.Infrastructure.Availability;
using PharmacyAvailability.Infrastructure.Persistence;
using Xunit;

namespace PharmacyAvailability.IntegrationTests;

// Uses only an explicitly supplied, disposable test database. Never application config.
public class ReceiverSqlServerTests : IAsyncLifetime
{
    private readonly string connection = Environment.GetEnvironmentVariable("INVENTORY_TEST_SQLSERVER")
        ?? "Server=(localdb)\\InventoryReceiverTests;Database=InventoryReceiverSafety;Integrated Security=true;TrustServerCertificate=true";
    private readonly string key = new('k', 40); // Synthetic test credential, never a publisher grant.
    private PharmacyDbContext Db() => new(new DbContextOptionsBuilder<PharmacyDbContext>().UseSqlServer(connection).Options);
    private Guid pharmacy, branch, medicine, installation;
    private InventorySnapshot Snapshot(long sequence = 1, int quantity = 8) => new(1, installation, branch,
        Guid.NewGuid(), sequence, DateTime.UtcNow, "partial", [new("test-product", medicine,
            new string('a',64), quantity, 45.25m, "EGP", "box", 20)]);
    private IConfiguration Config() => new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string,string?> {
        ["InventoryIntegration:Publishers:0:PharmacyId"] = pharmacy.ToString(),
        ["InventoryIntegration:Publishers:0:BranchId"] = branch.ToString(),
        ["InventoryIntegration:Publishers:0:InstallationId"] = installation.ToString(),
        ["InventoryIntegration:Publishers:0:KeySha256"] = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(key)))
    }).Build();
    private async Task<int> Send(InventorySnapshot snapshot)
    {
        await using var db = Db();
        var controller = new InventorySnapshotsController(db, Config()) {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() } };
        controller.Request.Scheme = "https";
        controller.Request.Headers["X-Inventory-Publisher-Key"] = key;
        var result = await controller.Receive(snapshot, default);
        return result switch { ObjectResult r => r.StatusCode ?? 200, StatusCodeResult r => r.StatusCode, _ => 500 };
    }
    public async Task InitializeAsync()
    {
        // Unique synthetic entities avoid sharing or erasing any existing data.
        await using var db = Db();
        await db.Database.MigrateAsync();
        var cat = new Category("ReceiverTest_" + Guid.NewGuid());
        var man = new Manufacturer("ReceiverTest_" + Guid.NewGuid());
        db.AddRange(cat, man); await db.SaveChangesAsync();
        var med = new Medicine("ReceiverTest_" + Guid.NewGuid(), "test", "test", "500mg", "Tablet",cat.Id,man.Id);
        var p = new Pharmacy("ReceiverTest_" + Guid.NewGuid(), "TEST_" + Guid.NewGuid(), "Synthetic", "Test",new(30,31), "0000000000");
        var b = p.AddBranch("Synthetic", "Synthetic", "Test", new(30,31), "0000000000");
        db.AddRange(med, p); await db.SaveChangesAsync();
        pharmacy=p.Id; branch=b.Id; medicine=med.Id; installation=Guid.NewGuid();
        db.Add(new InventoryPublisherBinding { BranchId=branch, ExternalProductId="test-product", InstallationId=installation,
            MedicineId=medicine, IdentityFingerprint=new string('a',64),UnitsPerPack=20,Verified=true });
        await db.SaveChangesAsync();
    }
    public Task DisposeAsync() => Task.CompletedTask;

    [Fact] public async Task ReplayPersistsAcrossContextsAndChangedPayloadConflicts()
    {
        var s=Snapshot(); Assert.Equal(200,await Send(s)); Assert.Equal(200,await Send(s));
        Assert.Equal(409,await Send(s with { Items=[s.Items[0] with { AvailableQuantity=1 }] }));
        await using var db=Db(); Assert.Equal(1,await db.Set<InventorySnapshotReceipt>().CountAsync(x=>x.BranchId==branch));
        Assert.Equal(8,(await db.PharmacyInventories.SingleAsync(x=>x.BranchId==branch)).AvailableQuantity);
    }
    [Fact] public async Task ReorderAndOlderObservationNeverOverwriteNewerStock()
    {
        var s=Snapshot(2,4); Assert.Equal(200,await Send(s)); Assert.Equal(409,await Send(Snapshot(1,99)));
        Assert.Equal(409,await Send(Snapshot(3,99) with { ObservedAtUtc=s.ObservedAtUtc.AddMinutes(-1) }));
        await using var db=Db(); var stock=await db.PharmacyInventories.SingleAsync(x=>x.BranchId==branch);
        Assert.Equal(4,stock.AvailableQuantity); Assert.Equal(s.ObservedAtUtc,stock.LastSuccessfulSyncAt);
    }
    [Fact] public async Task LaterInvalidItemRollsBackAllStockAndReceipt()
    {
        var s=Snapshot(); var invalid=s.Items[0] with {ExternalProductId="unreviewed",MedicineId=Guid.NewGuid()};
        Assert.Equal(409,await Send(s with {Items=[s.Items[0],invalid]}));
        await using var db=Db(); Assert.False(await db.PharmacyInventories.AnyAsync(x=>x.BranchId==branch));
        Assert.False(await db.Set<InventorySnapshotReceipt>().AnyAsync(x=>x.BranchId==branch));
    }
    [Fact] public async Task BranchAndInstallationCannotBeChangedByPublisher()
    {
        var s=Snapshot(); Assert.Equal(403,await Send(s with {BranchId=Guid.NewGuid()}));
        Assert.Equal(403,await Send(s with {InstallationId=Guid.NewGuid()}));
        await using var db=Db(); Assert.False(await db.PharmacyInventories.AnyAsync(x=>x.BranchId==branch));
    }
    [Fact] public async Task ConcurrentSameSnapshotCreatesOneReceiptAndIsRetryable()
    {
        var s=Snapshot(); var outcomes=await Task.WhenAll(Send(s),Send(s));
        Assert.All(outcomes,status=>Assert.Contains(status,new[]{200,409}));
        Assert.Contains(200,outcomes); Assert.Equal(200,await Send(s));
        await using var db=Db(); Assert.Equal(1,await db.Set<InventorySnapshotReceipt>().CountAsync(x=>x.BranchId==branch));
    }
    [Fact] public async Task ConcurrentSequencesCannotRegressFinalStock()
    {
        var first=Snapshot(1,1);var next=Snapshot(2,2) with {ObservedAtUtc=first.ObservedAtUtc.AddMilliseconds(10)};
        var outcomes=await Task.WhenAll(Send(first),Send(next));
        Assert.All(outcomes,status=>Assert.Contains(status,new[]{200,409}));
        Assert.Equal(200,await Send(next)); Assert.Equal(409,await Send(first with {SnapshotId=Guid.NewGuid()}));
        await using var db=Db(); Assert.Equal(2,(await db.PharmacyInventories.SingleAsync(x=>x.BranchId==branch)).AvailableQuantity);
    }
    [Fact] public async Task PublicZeroIsKnownAndStalePositiveIsUnknown()
    {
        Assert.Equal(200,await Send(Snapshot(1,0)));
        await using var db=Db(); var service=new AvailabilityService(db,NullLogger<AvailabilityService>.Instance);
        var result=await service.SearchAvailabilityAsync(new AvailabilitySearchRequest{MedicineId=medicine});
        Assert.Equal("OutOfStock",Assert.Single(result.Items).StockStatus);
        var stock=await db.PharmacyInventories.SingleAsync(x=>x.BranchId==branch);
        stock.UpdateFromSync(20,45.25m,DateTime.UtcNow.AddMinutes(-16));await db.SaveChangesAsync();
        result=await service.SearchAvailabilityAsync(new AvailabilitySearchRequest{MedicineId=medicine});
        var item=Assert.Single(result.Items);Assert.Equal("Unknown",item.StockStatus);Assert.Equal(0,item.AvailableQuantity);
    }
}
