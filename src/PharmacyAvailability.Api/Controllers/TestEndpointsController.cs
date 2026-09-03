using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PharmacyAvailability.Domain.Common.Exceptions;

namespace PharmacyAvailability.Api.Controllers;

[ApiController]
[Route("api/test")]
[ApiExplorerSettings(IgnoreApi = true)]
public class TestEndpointsController : ControllerBase
{
    [HttpGet("ok")]
    public IActionResult GetOk()
    {
        return Ok(new { message = "Healthy response" });
    }

    [HttpGet("domain-validation")]
    public IActionResult ThrowDomainValidation()
    {
        throw new DomainValidationException("Medicine name is required.");
    }

    [HttpGet("insufficient-stock")]
    public IActionResult ThrowInsufficientStock()
    {
        throw new InsufficientStockException(Guid.NewGuid(), 2, -5);
    }

    [HttpGet("invalid-price")]
    public IActionResult ThrowInvalidPrice()
    {
        throw new InvalidPriceException(-10.50m);
    }

    [HttpGet("concurrency-conflict")]
    public IActionResult ThrowConcurrencyConflict()
    {
        throw new DbUpdateConcurrencyException("Concurrency violation triggered.");
    }

    [HttpGet("unhandled-error")]
    public IActionResult ThrowUnhandledError()
    {
        throw new InvalidOperationException("Simulated unexpected database or server crash with internal secrets: password=secret123; Server=10.0.0.1;");
    }
}
