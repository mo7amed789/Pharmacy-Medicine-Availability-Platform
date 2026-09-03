using PharmacyAvailability.Domain.Common;
using PharmacyAvailability.Domain.Common.Exceptions;

namespace PharmacyAvailability.Domain.Inventory;

public class PriceHistory : BaseEntity
{
    public Guid PharmacyInventoryId { get; private set; }
    public decimal OldPrice { get; private set; }
    public decimal NewPrice { get; private set; }
    public Guid ChangedByUserId { get; private set; }

    // Required by EF Core
    protected PriceHistory() { }

    public PriceHistory(
        Guid pharmacyInventoryId,
        decimal oldPrice,
        decimal newPrice,
        Guid changedByUserId)
    {
        if (pharmacyInventoryId == Guid.Empty)
            throw new DomainValidationException("PharmacyInventoryId is required.");
        if (oldPrice < 0)
            throw new InvalidPriceException(oldPrice);
        if (newPrice < 0)
            throw new InvalidPriceException(newPrice);
        if (changedByUserId == Guid.Empty)
            throw new DomainValidationException("ChangedByUserId is required.");

        PharmacyInventoryId = pharmacyInventoryId;
        OldPrice = oldPrice;
        NewPrice = newPrice;
        ChangedByUserId = changedByUserId;
    }
}
