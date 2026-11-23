namespace PharmacyAvailability.Domain.Integration;

// Provisioned by an operator after exact product/pack review; never client-writable.
public class InventoryPublisherBinding
{
    public Guid BranchId { get; set; }
    public string ExternalProductId { get; set; } = "";
    public Guid InstallationId { get; set; }
    public Guid MedicineId { get; set; }
    public string IdentityFingerprint { get; set; } = "";
    public int UnitsPerPack { get; set; }
    public bool Verified { get; set; }
}

public class InventorySnapshotReceipt
{
    public Guid BranchId { get; set; }
    public Guid InstallationId { get; set; }
    public Guid SnapshotId { get; set; }
    public long Sequence { get; set; }
    public string PayloadHash { get; set; } = "";
    public int ItemsAccepted { get; set; }
    public DateTime ObservedAtUtc { get; set; }
    public DateTime ReceivedAtUtc { get; set; }
}
