namespace PharmacyAvailability.Contracts.Catalog;

public record CategoryDto(
    Guid Id,
    string Name,
    string? Description,
    bool IsActive);

public record ManufacturerDto(
    Guid Id,
    string Name,
    string? Country,
    bool IsActive);

public record BarcodeDto(
    Guid Id,
    string Barcode,
    bool IsPrimary);

public record AliasDto(
    Guid Id,
    string Alias);

public record MedicineDto(
    Guid Id,
    string BrandName,
    string GenericName,
    string ActiveIngredient,
    string Strength,
    string DosageForm,
    Guid CategoryId,
    string CategoryName,
    Guid ManufacturerId,
    string ManufacturerName,
    bool RequiresPrescription,
    bool IsActive,
    IReadOnlyList<BarcodeDto> Barcodes,
    IReadOnlyList<AliasDto> Aliases,
    DateTime CreatedAt);

public record PagedResult<T>(
    IReadOnlyList<T> Items,
    int Page,
    int PageSize,
    int TotalCount,
    int TotalPages,
    bool HasPreviousPage,
    bool HasNextPage);

public record CreateMedicineRequest(
    string BrandName,
    string GenericName,
    string ActiveIngredient,
    string Strength,
    string DosageForm,
    Guid CategoryId,
    Guid ManufacturerId,
    bool RequiresPrescription,
    string? InitialBarcode = null);

public record UpdateMedicineRequest(
    string BrandName,
    string GenericName,
    string ActiveIngredient,
    string Strength,
    string DosageForm,
    Guid CategoryId,
    Guid ManufacturerId,
    bool RequiresPrescription);

public record UpdateMedicineStatusRequest(
    bool IsActive);

public record CreateCategoryRequest(
    string Name,
    string? Description);

public record UpdateCategoryRequest(
    string Name,
    string? Description,
    bool IsActive);

public record CreateManufacturerRequest(
    string Name,
    string Country);

public record UpdateManufacturerRequest(
    string Name,
    string Country,
    bool IsActive);

public record AddBarcodeRequest(
    string Barcode,
    bool IsPrimary = false);

public record AddAliasRequest(
    string Alias);
