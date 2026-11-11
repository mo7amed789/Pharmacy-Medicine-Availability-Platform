using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PharmacyAvailability.Api.Filters;
using PharmacyAvailability.Application.Catalog;
using PharmacyAvailability.Contracts.Catalog;

namespace PharmacyAvailability.Api.Controllers;

[ApiController]
[Route("api/v1/admin")]
[Authorize(Policy = "PlatformAdminOnly")]
[FeatureGate("EnableManagementApis")]
public class AdminCatalogController : ControllerBase
{
    private readonly ICatalogService _catalogService;

    public AdminCatalogController(ICatalogService catalogService)
    {
        _catalogService = catalogService;
    }

    [HttpPost("medicines")]
    public async Task<ActionResult<MedicineDto>> CreateMedicine(
        [FromBody] CreateMedicineRequest request,
        CancellationToken ct)
    {
        var medicine = await _catalogService.CreateMedicineAsync(request, ct);
        return CreatedAtAction(
            actionName: nameof(MedicinesController.GetById),
            controllerName: "Medicines",
            routeValues: new { id = medicine.Id },
            value: medicine);
    }

    [HttpPut("medicines/{id:guid}")]
    public async Task<ActionResult<MedicineDto>> UpdateMedicine(
        [FromRoute] Guid id,
        [FromBody] UpdateMedicineRequest request,
        CancellationToken ct)
    {
        var updated = await _catalogService.UpdateMedicineAsync(id, request, ct);
        return Ok(updated);
    }

    [HttpPatch("medicines/{id:guid}/status")]
    public async Task<ActionResult<MedicineDto>> UpdateMedicineStatus(
        [FromRoute] Guid id,
        [FromBody] UpdateMedicineStatusRequest request,
        CancellationToken ct)
    {
        var updated = await _catalogService.UpdateMedicineStatusAsync(id, request, ct);
        return Ok(updated);
    }

    [HttpPost("categories")]
    public async Task<ActionResult<CategoryDto>> CreateCategory(
        [FromBody] CreateCategoryRequest request,
        CancellationToken ct)
    {
        var category = await _catalogService.CreateCategoryAsync(request, ct);
        return CreatedAtAction(
            actionName: nameof(CategoriesController.GetCategories),
            controllerName: "Categories",
            routeValues: null,
            value: category);
    }

    [HttpPut("categories/{id:guid}")]
    public async Task<ActionResult<CategoryDto>> UpdateCategory(
        [FromRoute] Guid id,
        [FromBody] UpdateCategoryRequest request,
        CancellationToken ct)
    {
        var category = await _catalogService.UpdateCategoryAsync(id, request, ct);
        return Ok(category);
    }

    [HttpPost("manufacturers")]
    public async Task<ActionResult<ManufacturerDto>> CreateManufacturer(
        [FromBody] CreateManufacturerRequest request,
        CancellationToken ct)
    {
        var manufacturer = await _catalogService.CreateManufacturerAsync(request, ct);
        return CreatedAtAction(
            actionName: nameof(ManufacturersController.GetManufacturers),
            controllerName: "Manufacturers",
            routeValues: null,
            value: manufacturer);
    }

    [HttpPut("manufacturers/{id:guid}")]
    public async Task<ActionResult<ManufacturerDto>> UpdateManufacturer(
        [FromRoute] Guid id,
        [FromBody] UpdateManufacturerRequest request,
        CancellationToken ct)
    {
        var manufacturer = await _catalogService.UpdateManufacturerAsync(id, request, ct);
        return Ok(manufacturer);
    }

    [HttpPost("medicines/{id:guid}/barcodes")]
    public async Task<ActionResult<BarcodeDto>> AddBarcode(
        [FromRoute] Guid id,
        [FromBody] AddBarcodeRequest request,
        CancellationToken ct)
    {
        var barcode = await _catalogService.AddBarcodeAsync(id, request, ct);
        return CreatedAtAction(
            actionName: nameof(MedicinesController.GetByBarcode),
            controllerName: "Medicines",
            routeValues: new { barcode = barcode.Barcode },
            value: barcode);
    }

    [HttpPost("medicines/{id:guid}/aliases")]
    public async Task<ActionResult<AliasDto>> AddAlias(
        [FromRoute] Guid id,
        [FromBody] AddAliasRequest request,
        CancellationToken ct)
    {
        var alias = await _catalogService.AddAliasAsync(id, request, ct);
        return Ok(alias);
    }
}
