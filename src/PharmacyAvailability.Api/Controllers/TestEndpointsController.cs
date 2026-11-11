using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using PharmacyAvailability.Domain.Common.Exceptions;

namespace PharmacyAvailability.Api.Controllers;

[ApiController]
[Route("api/test")]
[ApiExplorerSettings(IgnoreApi = true)]
[AllowAnonymous]
public class TestEndpointsController : ControllerBase
{
    private readonly IHostEnvironment _environment;

    public TestEndpointsController(IHostEnvironment environment)
    {
        _environment = environment;
    }

    private IActionResult? BlockOutsideDevelopment() =>
        _environment.IsDevelopment() ? null : NotFound();

    [HttpGet("ok")]
    public IActionResult GetOk()
    {
        if (BlockOutsideDevelopment() is { } blocked)
            return blocked;

        return Ok(new { message = "Healthy response" });
    }

    [HttpGet("domain-validation")]
    public IActionResult ThrowDomainValidation()
    {
        if (BlockOutsideDevelopment() is { } blocked)
            return blocked;

        throw new DomainValidationException("Medicine name is required.");
    }

    [HttpGet("insufficient-stock")]
    public IActionResult ThrowInsufficientStock()
    {
        if (BlockOutsideDevelopment() is { } blocked)
            return blocked;

        throw new InsufficientStockException(Guid.NewGuid(), 2, -5);
    }

    [HttpGet("invalid-price")]
    public IActionResult ThrowInvalidPrice()
    {
        if (BlockOutsideDevelopment() is { } blocked)
            return blocked;

        throw new InvalidPriceException(-10.50m);
    }

    [HttpGet("concurrency-conflict")]
    public IActionResult ThrowConcurrencyConflict()
    {
        if (BlockOutsideDevelopment() is { } blocked)
            return blocked;

        throw new DbUpdateConcurrencyException("Concurrency violation triggered.");
    }

    [HttpGet("unhandled-error")]
    public IActionResult ThrowUnhandledError()
    {
        if (BlockOutsideDevelopment() is { } blocked)
            return blocked;

        throw new InvalidOperationException("Simulated unexpected database or server crash with internal secrets: password=secret123; Server=10.0.0.1;");
    }
}
