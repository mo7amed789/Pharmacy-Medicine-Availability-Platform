namespace PharmacyAvailability.Contracts.Operations;

public record BranchDashboardDto(
    Guid PharmacyId,
    string PharmacyName,
    Guid BranchId,
    string BranchName,
    int ActiveReservationsCount,
    int ExpiringSoonReservationsCount,
    int TodayConfirmedPickupsCount,
    int TodayCancelledReservationsCount,
    int TodayExpiredReservationsCount,
    int LowStockItemsCount,
    int TotalActiveMedicinesCount,
    DateTime GeneratedAtUtc);

public record StaffReservationQueueItemDto(
    Guid ReservationId,
    Guid CustomerId,
    string CustomerName,
    string CustomerPhone,
    string CustomerEmail,
    Guid MedicineId,
    string MedicineBrandName,
    string MedicineGenericName,
    string DosageForm,
    string Strength,
    int Quantity,
    decimal UnitPrice,
    decimal TotalAmountDue,
    string Status,
    DateTime CreatedAt,
    DateTime ExpiresAt,
    int RemainingMinutes,
    bool IsExpiringSoon);

public record StaffReservationQueueQuery
{
    public string? Search { get; init; }
    public string? Status { get; init; } = "Active";
    public bool? ExpiringSoonOnly { get; init; }
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 20;
}

public record LowStockAlertDto(
    Guid InventoryId,
    Guid MedicineId,
    string MedicineBrandName,
    string MedicineGenericName,
    string CategoryName,
    string ManufacturerName,
    int AvailableQuantity,
    int ReservedQuantity,
    int TotalPhysicalStock,
    decimal Price,
    int Threshold,
    DateTime LastUpdated);

public record BranchActivityFeedItemDto(
    Guid Id,
    string ActivityType,
    string Description,
    Guid? MedicineId,
    string? MedicineBrandName,
    int? DeltaQuantity,
    int? ResultingQuantity,
    string? ActorRole,
    DateTime Timestamp);
