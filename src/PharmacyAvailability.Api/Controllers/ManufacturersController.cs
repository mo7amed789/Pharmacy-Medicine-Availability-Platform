using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using PharmacyAvailability.Application.Catalog;
using PharmacyAvailability.Contracts.Catalog;

namespace PharmacyAvailability.Api.Controllers;

[ApiController]
[Route("api/v1/manufacturers")]
[AllowAnonymous]
public class ManufacturersController : ControllerBase
{
    private readonly ICatalogService _catalogService;

    public ManufacturersController(ICatalogService catalogService)
    {
        _catalogService = catalogService;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<ManufacturerDto>>> GetManufacturers(
        [FromQuery] bool activeOnly = true,
        CancellationToken ct = default)
    {
        var manufacturers = await _catalogService.GetManufacturersAsync(activeOnly, ct);
        return Ok(manufacturers);
    }
}
