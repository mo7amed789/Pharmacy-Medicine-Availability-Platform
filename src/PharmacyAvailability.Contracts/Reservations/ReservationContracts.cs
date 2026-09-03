namespace PharmacyAvailability.Contracts.Reservations;

public record CreateReservationRequest(
    Guid BranchId,
    Guid MedicineId,
    int Quantity);

public record ReservationDto(
    Guid Id,
    Guid CustomerId,
    Guid PharmacyId,
    string PharmacyName,
    Guid BranchId,
    string BranchName,
    string BranchAddress,
    Guid MedicineId,
    string MedicineBrandName,
    string MedicineGenericName,
    int Quantity,
    string Status,
    DateTime CreatedAt,
    DateTime ExpiresAt,
    DateTime? ConfirmedAt,
    DateTime? ReleasedAt,
    DateTime? CancelledAt,
    DateTime? ExpiredAt);

public record AdminReleaseReservationRequest(string Reason);

public record GetReservationsQuery
{
    public string? Status { get; init; }
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 20;
}
