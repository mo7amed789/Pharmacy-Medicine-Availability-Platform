using PharmacyAvailability.Contracts.Catalog;

namespace PharmacyAvailability.Application.Catalog;

public interface ICatalogService
{
    Task<PagedResult<MedicineDto>> SearchMedicinesAsync(
        string? searchTerm,
        Guid? categoryId,
        Guid? manufacturerId,
        bool activeOnly = true,
        int page = 1,
        int pageSize = 20,
        string? sortBy = null,
        string? sortOrder = null,
        CancellationToken ct = default);

    Task<MedicineDto?> GetMedicineByIdAsync(Guid id, CancellationToken ct = default);
    Task<MedicineDto?> GetMedicineByBarcodeAsync(string barcode, CancellationToken ct = default);
    Task<IReadOnlyList<MedicineDto>> GetMedicinesByAliasAsync(string alias, CancellationToken ct = default);

    Task<IReadOnlyList<CategoryDto>> GetCategoriesAsync(bool activeOnly = true, CancellationToken ct = default);
    Task<IReadOnlyList<ManufacturerDto>> GetManufacturersAsync(bool activeOnly = true, CancellationToken ct = default);

    Task<MedicineDto> CreateMedicineAsync(CreateMedicineRequest request, CancellationToken ct = default);
    Task<MedicineDto> UpdateMedicineAsync(Guid id, UpdateMedicineRequest request, CancellationToken ct = default);
    Task<MedicineDto> UpdateMedicineStatusAsync(Guid id, UpdateMedicineStatusRequest request, CancellationToken ct = default);

    Task<CategoryDto> CreateCategoryAsync(CreateCategoryRequest request, CancellationToken ct = default);
    Task<CategoryDto> UpdateCategoryAsync(Guid id, UpdateCategoryRequest request, CancellationToken ct = default);

    Task<ManufacturerDto> CreateManufacturerAsync(CreateManufacturerRequest request, CancellationToken ct = default);
    Task<ManufacturerDto> UpdateManufacturerAsync(Guid id, UpdateManufacturerRequest request, CancellationToken ct = default);

    Task<BarcodeDto> AddBarcodeAsync(Guid medicineId, AddBarcodeRequest request, CancellationToken ct = default);
    Task<AliasDto> AddAliasAsync(Guid medicineId, AddAliasRequest request, CancellationToken ct = default);
}
