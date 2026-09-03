using PharmacyAvailability.Domain.Common;
using PharmacyAvailability.Domain.Common.Exceptions;

namespace PharmacyAvailability.Domain.Inventory;

public class InventoryTransaction : BaseEntity
{
    public Guid PharmacyInventoryId { get; private set; }
    public int DeltaQuantity { get; private set; }
    public int ResultingQuantity { get; private set; }
    public InventoryAdjustmentReason Reason { get; private set; }
    public Guid ActorUserId { get; private set; }
    public string? Notes { get; private set; }

    // Required by EF Core
    protected InventoryTransaction() { }

    public InventoryTransaction(
        Guid pharmacyInventoryId,
        int deltaQuantity,
        int resultingQuantity,
        InventoryAdjustmentReason reason,
        Guid actorUserId,
        string? notes = null)
    {
        if (pharmacyInventoryId == Guid.Empty)
            throw new DomainValidationException("PharmacyInventoryId is required.");
        if (resultingQuantity < 0)
            throw new DomainValidationException($"Resulting quantity cannot be negative ({resultingQuantity}).");
        if (actorUserId == Guid.Empty)
            throw new DomainValidationException("ActorUserId is required.");

        PharmacyInventoryId = pharmacyInventoryId;
        DeltaQuantity = deltaQuantity;
        ResultingQuantity = resultingQuantity;
        Reason = reason;
        ActorUserId = actorUserId;
        Notes = notes?.Trim();
    }
}
