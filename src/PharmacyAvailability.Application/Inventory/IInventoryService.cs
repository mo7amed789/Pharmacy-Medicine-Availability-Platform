using PharmacyAvailability.Contracts.Inventory;

namespace PharmacyAvailability.Application.Inventory;

public interface IInventoryService
{
    Task<IReadOnlyList<InventoryItemDto>> GetBranchInventoryAsync(
        Guid pharmacyId,
        Guid branchId,
        CancellationToken ct = default);

    Task<InventoryItemDto?> GetInventoryItemByMedicineAsync(
        Guid pharmacyId,
        Guid branchId,
        Guid medicineId,
        CancellationToken ct = default);

    Task<InventoryItemDto> AddInventoryItemAsync(
        Guid pharmacyId,
        Guid branchId,
        CreateInventoryItemRequest request,
        Guid actorUserId,
        CancellationToken ct = default);

    Task<InventoryItemDto> AdjustStockAsync(
        Guid pharmacyId,
        Guid branchId,
        Guid medicineId,
        AdjustStockRequest request,
        Guid actorUserId,
        CancellationToken ct = default);

    Task<InventoryItemDto> UpdatePriceAsync(
        Guid pharmacyId,
        Guid branchId,
        Guid medicineId,
        UpdatePriceRequest request,
        Guid actorUserId,
        CancellationToken ct = default);

    Task<InventoryItemDto> ReserveStockAsync(
        Guid pharmacyId,
        Guid branchId,
        Guid medicineId,
        ReservationRequest request,
        Guid actorUserId,
        CancellationToken ct = default);

    Task<InventoryItemDto> ReleaseReservationAsync(
        Guid pharmacyId,
        Guid branchId,
        Guid medicineId,
        ReservationRequest request,
        Guid actorUserId,
        CancellationToken ct = default);

    Task<InventoryItemDto> ConfirmReservationAsync(
        Guid pharmacyId,
        Guid branchId,
        Guid medicineId,
        ReservationRequest request,
        Guid actorUserId,
        CancellationToken ct = default);

    Task<IReadOnlyList<InventoryTransactionDto>> GetTransactionsAsync(
        Guid pharmacyId,
        Guid branchId,
        Guid medicineId,
        CancellationToken ct = default);

    Task<IReadOnlyList<PriceHistoryDto>> GetPriceHistoryAsync(
        Guid pharmacyId,
        Guid branchId,
        Guid medicineId,
        CancellationToken ct = default);
}
