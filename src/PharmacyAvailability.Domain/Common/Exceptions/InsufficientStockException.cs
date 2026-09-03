namespace PharmacyAvailability.Domain.Common.Exceptions;

public class InsufficientStockException : DomainException
{
    public Guid PharmacyInventoryId { get; }
    public int AvailableQuantity { get; }
    public int RequestedDelta { get; }

    public InsufficientStockException(Guid pharmacyInventoryId, int availableQuantity, int requestedDelta)
        : base($"Insufficient stock for inventory '{pharmacyInventoryId}'. Available: {availableQuantity}, Requested adjustment: {requestedDelta}.")
    {
        PharmacyInventoryId = pharmacyInventoryId;
        AvailableQuantity = availableQuantity;
        RequestedDelta = requestedDelta;
    }
}
