using System.Data;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PharmacyAvailability.Api.Filters;
using PharmacyAvailability.Domain.Integration;
using PharmacyAvailability.Domain.Inventory;
using PharmacyAvailability.Infrastructure.Persistence;

namespace PharmacyAvailability.Api.Integration;

public record SnapshotItem(string ExternalProductId, Guid MedicineId, string IdentityFingerprint,
    int AvailableQuantity, [property: JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)] decimal Price, string Currency, string SaleUnit, int UnitsPerPack);
public record InventorySnapshot(int SchemaVersion, Guid InstallationId, Guid BranchId,
    Guid SnapshotId, long Sequence, DateTime ObservedAtUtc, string Mode, SnapshotItem[] Items);
public record PublisherGrant(Guid PharmacyId, Guid BranchId, Guid InstallationId, string KeySha256);

public static class SnapshotRules
{
    public static bool KeyMatches(string? supplied, string? expectedHash)
    {
        if (string.IsNullOrWhiteSpace(supplied) || supplied.Length > 256 || expectedHash?.Length != 64)
            return false;
        try { return CryptographicOperations.FixedTimeEquals(
            SHA256.HashData(Encoding.UTF8.GetBytes(supplied)), Convert.FromHexString(expectedHash)); }
        catch (FormatException) { return false; }
    }

    public static string? Validate(InventorySnapshot s, DateTime now)
    {
        if (s.SchemaVersion != 1 || s.Mode != "partial" || s.InstallationId == Guid.Empty ||
            s.BranchId == Guid.Empty || s.SnapshotId == Guid.Empty || s.Sequence <= 0)
            return "Invalid snapshot envelope.";
        if (s.ObservedAtUtc.Kind != DateTimeKind.Utc || s.ObservedAtUtc > now.AddMinutes(2) ||
            s.ObservedAtUtc < now.AddMinutes(-15)) return "Observation must be UTC and no older than 15 minutes.";
        if (s.Items == null || s.Items.Length is < 1 or > 500) return "Send 1 to 500 items.";
        if (s.Items.Any(i => i == null || string.IsNullOrWhiteSpace(i.ExternalProductId) ||
            i.ExternalProductId.Length > 100 || i.MedicineId == Guid.Empty ||
            i.IdentityFingerprint?.Length != 64 || !i.IdentityFingerprint.All(Uri.IsHexDigit) ||
            i.AvailableQuantity < 0 || i.Price <= 0 || decimal.Round(i.Price, 2) != i.Price ||
            i.Price > 99999999 || i.Currency != "EGP" || i.SaleUnit != "box" || i.UnitsPerPack <= 0))
            return "Invalid item, price, fingerprint or box unit.";
        if (s.Items.Select(i => i.ExternalProductId).Distinct().Count() != s.Items.Length ||
            s.Items.Select(i => i.MedicineId).Distinct().Count() != s.Items.Length)
            return "Duplicate product or medicine.";
        return null;
    }
}

// No fallback to end-user JWT or anonymous writes. Key hashes are provisioned out of band.
[ApiController]
[Route("api/v1/integration/inventory-snapshots")]
[FeatureGate("EnableInventoryIntegration")]
[AllowAnonymous] // The dedicated publisher credential below is mandatory on every request.
[RequestSizeLimit(512 * 1024)]
public class InventorySnapshotsController(PharmacyDbContext db, IConfiguration config) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> Receive([FromBody] InventorySnapshot snapshot, CancellationToken ct)
    {
        if (!Request.IsHttps) return BadRequest(new { error = "HTTPS required." });
        var key = Request.Headers["X-Inventory-Publisher-Key"].ToString();
        if (key.Length < 32) return Unauthorized();
        var grants = config.GetSection("InventoryIntegration:Publishers").Get<PublisherGrant[]>() ?? [];
        var matched = grants.Where(g => SnapshotRules.KeyMatches(key, g.KeySha256)).ToArray();
        var grant = matched.Length == 1 ? matched[0] : null;
        if (grant == null) return Unauthorized();
        if (grant.PharmacyId == Guid.Empty || grant.BranchId != snapshot.BranchId ||
            grant.InstallationId != snapshot.InstallationId) return StatusCode(StatusCodes.Status403Forbidden);
        
        // Reservations require a local acknowledgement protocol, not snapshot overwrites.
        if (config.GetValue<bool>("FeatureFlags:EnableReservations"))
            return Conflict(new { error = "Snapshot integration requires reservations disabled." });
        var hash = Convert.ToHexString(SHA256.HashData(JsonSerializer.SerializeToUtf8Bytes(snapshot)));
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        try
        {
            var receipts = db.Set<InventorySnapshotReceipt>();
            var previous = await receipts.SingleOrDefaultAsync(r => r.BranchId == grant.BranchId &&
                r.InstallationId == grant.InstallationId && r.SnapshotId == snapshot.SnapshotId, ct);
            if (previous != null)
                return previous.PayloadHash == hash ? Ok(new { previous.SnapshotId, previous.Sequence,
                    accepted = previous.ItemsAccepted, replay = true }) : Conflict(new { error = "Snapshot ID reused." });
            var error = SnapshotRules.Validate(snapshot, DateTime.UtcNow);
            if (error != null) return BadRequest(new { error });
            if (await receipts.AnyAsync(r => r.BranchId == grant.BranchId && r.Sequence >= snapshot.Sequence, ct))
                return Conflict(new { error = "Sequence must increase for the branch." });
            if (!await db.PharmacyBranches.AnyAsync(b => b.Id == grant.BranchId &&
                b.PharmacyId == grant.PharmacyId && b.IsActive &&
                b.Pharmacy!.Status == PharmacyAvailability.Domain.Pharmacies.PharmacyStatus.Active, ct))
                return StatusCode(StatusCodes.Status403Forbidden);
            foreach (var item in snapshot.Items)
            {
                var binding = await db.Set<InventoryPublisherBinding>().SingleOrDefaultAsync(b =>
                    b.BranchId == grant.BranchId && b.ExternalProductId == item.ExternalProductId, ct);
                if (binding == null || !binding.Verified || binding.InstallationId != grant.InstallationId ||
                    binding.MedicineId != item.MedicineId || binding.IdentityFingerprint != item.IdentityFingerprint ||
                    binding.UnitsPerPack != item.UnitsPerPack)
                    return Conflict(new { error = "Missing or changed reviewed binding.", item.ExternalProductId });
                if (!await db.Medicines.AnyAsync(m => m.Id == item.MedicineId && m.IsActive, ct))
                    return Conflict(new { error = "Medicine inactive or absent." });
                var stock = await db.PharmacyInventories.SingleOrDefaultAsync(i =>
                    i.BranchId == grant.BranchId && i.MedicineId == item.MedicineId, ct);
                if (stock == null)
                {
                    stock = new PharmacyInventory(grant.PharmacyId, grant.BranchId, item.MedicineId,
                        0, item.Price, "EGP", InventorySource.IntegrationSync);
                    db.PharmacyInventories.Add(stock);
                }
                if (stock.LastSuccessfulSyncAt.HasValue && stock.LastSuccessfulSyncAt > snapshot.ObservedAtUtc)
                    return Conflict(new { error = "Observation older than accepted inventory." });
                if (stock.PharmacyId != grant.PharmacyId || stock.ReservedQuantity != 0 || stock.Currency != "EGP")
                    return Conflict(new { error = "Inventory ownership, reservation or currency conflict." });
                stock.UpdateFromSync(item.AvailableQuantity, item.Price, snapshot.ObservedAtUtc);
            }
            receipts.Add(new InventorySnapshotReceipt { BranchId = grant.BranchId,
                InstallationId = grant.InstallationId, SnapshotId = snapshot.SnapshotId,
                Sequence = snapshot.Sequence, PayloadHash = hash, ItemsAccepted = snapshot.Items.Length,
                ObservedAtUtc = snapshot.ObservedAtUtc, ReceivedAtUtc = DateTime.UtcNow });
            await db.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);
            return Ok(new { snapshot.SnapshotId, snapshot.Sequence, accepted = snapshot.Items.Length, replay = false });
        }
        catch (DbUpdateException) { return Conflict(new { error = "Concurrent update. Retry the same snapshot." }); }
    }
}
