using Microsoft.AspNetCore.Mvc;
using PharmacyAvailability.Application.Catalog;
using PharmacyAvailability.Contracts.Catalog;

namespace PharmacyAvailability.Api.Controllers;

[ApiController]
[Route("api/v1/categories")]
public class CategoriesController : ControllerBase
{
    private readonly ICatalogService _catalogService;

    public CategoriesController(ICatalogService catalogService)
    {
        _catalogService = catalogService;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<CategoryDto>>> GetCategories(
        [FromQuery] bool activeOnly = true,
        CancellationToken ct = default)
    {
        var categories = await _catalogService.GetCategoriesAsync(activeOnly, ct);
        return Ok(categories);
    }
}
