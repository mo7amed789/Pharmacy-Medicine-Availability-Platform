namespace PharmacyAvailability.Contracts.Inventory;

public record InventoryItemDto(
    Guid Id,
    Guid PharmacyId,
    Guid BranchId,
    Guid MedicineId,
    string MedicineBrandName,
    string MedicineGenericName,
    int AvailableQuantity,
    int ReservedQuantity,
    int TotalStock,
    decimal Price,
    string Currency,
    string Source,
    string Freshness,
    DateTime UpdatedAt);

public record CreateInventoryItemRequest(
    Guid MedicineId,
    int InitialQuantity,
    decimal Price,
    string Currency = "EGP");

public record AdjustStockRequest(
    int DeltaQuantity,
    string Reason = "ManualAdjustment",
    string? Notes = null);

public record UpdatePriceRequest(
    decimal NewPrice);

public record ReservationRequest(
    int Quantity,
    string? Notes = null);

public record InventoryTransactionDto(
    Guid Id,
    Guid PharmacyInventoryId,
    int DeltaQuantity,
    int ResultingQuantity,
    string Reason,
    Guid ActorUserId,
    string? Notes,
    DateTime CreatedAt);

public record PriceHistoryDto(
    Guid Id,
    Guid PharmacyInventoryId,
    decimal OldPrice,
    decimal NewPrice,
    Guid ChangedByUserId,
    DateTime CreatedAt);
