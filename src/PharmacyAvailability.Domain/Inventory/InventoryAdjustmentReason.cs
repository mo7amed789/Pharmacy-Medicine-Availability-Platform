namespace PharmacyAvailability.Domain.Inventory;

public enum InventoryAdjustmentReason
{
    Sale = 1,
    Restock = 2,
    ManualAdjustment = 3,
    Correction = 4,
    Return = 5,
    Discard = 6,
    SyncUpdate = 7,
    ReservationHold = 8,
    ReservationRelease = 9,
    ReservationConfirmed = 10
}
