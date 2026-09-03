namespace PharmacyAvailability.Contracts.Availability;

public record MedicineAvailabilityDto(
    Guid InventoryId,
    Guid MedicineId,
    string MedicineBrandName,
    string MedicineGenericName,
    string Strength,
    string DosageForm,
    Guid PharmacyId,
    string PharmacyName,
    Guid BranchId,
    string BranchName,
    string BranchAddress,
    string BranchCity,
    string BranchPhone,
    string? OpeningHours,
    double BranchLatitude,
    double BranchLongitude,
    double? DistanceKm,
    int AvailableQuantity,
    string StockStatus,
    decimal Price,
    string Currency,
    string Freshness,
    DateTime LastUpdatedAt);

public record AvailabilitySearchRequest
{
    public Guid? MedicineId { get; init; }
    public string? Barcode { get; init; }
    public string? Alias { get; init; }
    public string? Q { get; init; }
    public string? Query { get; init; }
    public double? Latitude { get; init; }
    public double? Longitude { get; init; }
    public double? RadiusKm { get; init; }
    public string? Sort { get; init; }
    public string? SortBy { get; init; }
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 20;

    public string? EffectiveQuery => !string.IsNullOrWhiteSpace(Q) ? Q : Query;
    public string? EffectiveSortBy => !string.IsNullOrWhiteSpace(Sort) ? Sort : SortBy;
}

public record AvailabilitySearchResult(
    IReadOnlyList<MedicineAvailabilityDto> Items,
    int Page,
    int PageSize,
    int TotalCount,
    int TotalPages,
    bool HasPreviousPage,
    bool HasNextPage);
