using Microsoft.AspNetCore.Mvc;
using PharmacyAvailability.Application.Catalog;
using PharmacyAvailability.Contracts.Catalog;

namespace PharmacyAvailability.Api.Controllers;

[ApiController]
[Route("api/v1/medicines")]
public class MedicinesController : ControllerBase
{
    private readonly ICatalogService _catalogService;

    public MedicinesController(ICatalogService catalogService)
    {
        _catalogService = catalogService;
    }

    [HttpGet]
    public async Task<ActionResult<PagedResult<MedicineDto>>> Search(
        [FromQuery] string? searchTerm,
        [FromQuery] Guid? categoryId,
        [FromQuery] Guid? manufacturerId,
        [FromQuery] bool activeOnly = true,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? sortBy = null,
        [FromQuery] string? sortOrder = null,
        CancellationToken ct = default)
    {
        var result = await _catalogService.SearchMedicinesAsync(
            searchTerm,
            categoryId,
            manufacturerId,
            activeOnly,
            page,
            pageSize,
            sortBy,
            sortOrder,
            ct);

        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<MedicineDto>> GetById([FromRoute] Guid id, CancellationToken ct)
    {
        var medicine = await _catalogService.GetMedicineByIdAsync(id, ct);
        if (medicine == null)
        {
            return NotFound(new ProblemDetails
            {
                Status = StatusCodes.Status404NotFound,
                Title = "Medicine Not Found",
                Detail = $"Medicine with ID '{id}' was not found.",
                Instance = HttpContext.Request.Path
            });
        }

        return Ok(medicine);
    }

    [HttpGet("barcode/{barcode}")]
    public async Task<ActionResult<MedicineDto>> GetByBarcode([FromRoute] string barcode, CancellationToken ct)
    {
        var medicine = await _catalogService.GetMedicineByBarcodeAsync(barcode, ct);
        if (medicine == null)
        {
            return NotFound(new ProblemDetails
            {
                Status = StatusCodes.Status404NotFound,
                Title = "Barcode Not Found",
                Detail = $"No medicine found matching barcode '{barcode}'.",
                Instance = HttpContext.Request.Path
            });
        }

        return Ok(medicine);
    }

    [HttpGet("alias/{alias}")]
    public async Task<ActionResult<IReadOnlyList<MedicineDto>>> GetByAlias([FromRoute] string alias, CancellationToken ct)
    {
        var medicines = await _catalogService.GetMedicinesByAliasAsync(alias, ct);
        if (medicines.Count == 0)
        {
            return NotFound(new ProblemDetails
            {
                Status = StatusCodes.Status404NotFound,
                Title = "Alias Not Found",
                Detail = $"No medicine found matching alias '{alias}'.",
                Instance = HttpContext.Request.Path
            });
        }

        return Ok(medicines);
    }
}
