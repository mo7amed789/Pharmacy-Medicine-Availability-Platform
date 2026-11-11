using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PharmacyAvailability.Api.Authorization;
using PharmacyAvailability.Api.Filters;
using PharmacyAvailability.Application.Inventory;
using PharmacyAvailability.Contracts.Inventory;

namespace PharmacyAvailability.Api.Controllers;

[ApiController]
[Route("api/v1/pharmacies/{pharmacyId:guid}/branches/{branchId:guid}/inventory")]
[Authorize]
[FeatureGate("EnableManagementApis")]
public class PharmacyInventoryController : ControllerBase
{
    private readonly IInventoryService _inventoryService;
    private readonly IAuthorizationService _authorizationService;

    public PharmacyInventoryController(
        IInventoryService inventoryService,
        IAuthorizationService authorizationService)
    {
        _inventoryService = inventoryService;
        _authorizationService = authorizationService;
    }

    private async Task<bool> AuthorizeAccessAsync(Guid pharmacyId)
    {
        var authResult = await _authorizationService.AuthorizeAsync(
            User,
            pharmacyId,
            new PharmacyOwnershipRequirement());

        return authResult.Succeeded;
    }

    private Guid GetActorUserId()
    {
        var claim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ??
                    User.FindFirst("sub")?.Value;

        return Guid.TryParse(claim, out var id) ? id : Guid.Empty;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<InventoryItemDto>>> GetBranchInventory(
        [FromRoute] Guid pharmacyId,
        [FromRoute] Guid branchId,
        CancellationToken ct)
    {
        if (!await AuthorizeAccessAsync(pharmacyId))
            return Forbid();

        var items = await _inventoryService.GetBranchInventoryAsync(pharmacyId, branchId, ct);
        return Ok(items);
    }

    [HttpGet("{medicineId:guid}")]
    public async Task<ActionResult<InventoryItemDto>> GetInventoryItem(
        [FromRoute] Guid pharmacyId,
        [FromRoute] Guid branchId,
        [FromRoute] Guid medicineId,
        CancellationToken ct)
    {
        if (!await AuthorizeAccessAsync(pharmacyId))
            return Forbid();

        var item = await _inventoryService.GetInventoryItemByMedicineAsync(pharmacyId, branchId, medicineId, ct);
        if (item == null)
        {
            return NotFound(new ProblemDetails
            {
                Status = StatusCodes.Status404NotFound,
                Title = "Inventory Item Not Found",
                Detail = $"No inventory item found for medicine '{medicineId}' at this branch.",
                Instance = HttpContext.Request.Path
            });
        }

        return Ok(item);
    }

    [HttpPost]
    public async Task<ActionResult<InventoryItemDto>> AddInventoryItem(
        [FromRoute] Guid pharmacyId,
        [FromRoute] Guid branchId,
        [FromBody] CreateInventoryItemRequest request,
        CancellationToken ct)
    {
        if (!await AuthorizeAccessAsync(pharmacyId))
            return Forbid();

        var item = await _inventoryService.AddInventoryItemAsync(
            pharmacyId,
            branchId,
            request,
            GetActorUserId(),
            ct);

        return CreatedAtAction(
            actionName: nameof(GetInventoryItem),
            routeValues: new { pharmacyId, branchId, medicineId = item.MedicineId },
            value: item);
    }

    [HttpPatch("{medicineId:guid}/quantity")]
    public async Task<ActionResult<InventoryItemDto>> AdjustStock(
        [FromRoute] Guid pharmacyId,
        [FromRoute] Guid branchId,
        [FromRoute] Guid medicineId,
        [FromBody] AdjustStockRequest request,
        CancellationToken ct)
    {
        if (!await AuthorizeAccessAsync(pharmacyId))
            return Forbid();

        var item = await _inventoryService.AdjustStockAsync(
            pharmacyId,
            branchId,
            medicineId,
            request,
            GetActorUserId(),
            ct);

        return Ok(item);
    }

    [HttpPatch("{medicineId:guid}/price")]
    public async Task<ActionResult<InventoryItemDto>> UpdatePrice(
        [FromRoute] Guid pharmacyId,
        [FromRoute] Guid branchId,
        [FromRoute] Guid medicineId,
        [FromBody] UpdatePriceRequest request,
        CancellationToken ct)
    {
        if (!await AuthorizeAccessAsync(pharmacyId))
            return Forbid();

        var item = await _inventoryService.UpdatePriceAsync(
            pharmacyId,
            branchId,
            medicineId,
            request,
            GetActorUserId(),
            ct);

        return Ok(item);
    }

    [HttpPost("{medicineId:guid}/reserve")]
    public async Task<ActionResult<InventoryItemDto>> ReserveStock(
        [FromRoute] Guid pharmacyId,
        [FromRoute] Guid branchId,
        [FromRoute] Guid medicineId,
        [FromBody] ReservationRequest request,
        CancellationToken ct)
    {
        if (!await AuthorizeAccessAsync(pharmacyId))
            return Forbid();

        var item = await _inventoryService.ReserveStockAsync(
            pharmacyId,
            branchId,
            medicineId,
            request,
            GetActorUserId(),
            ct);

        return Ok(item);
    }

    [HttpPost("{medicineId:guid}/release")]
    public async Task<ActionResult<InventoryItemDto>> ReleaseReservation(
        [FromRoute] Guid pharmacyId,
        [FromRoute] Guid branchId,
        [FromRoute] Guid medicineId,
        [FromBody] ReservationRequest request,
        CancellationToken ct)
    {
        if (!await AuthorizeAccessAsync(pharmacyId))
            return Forbid();

        var item = await _inventoryService.ReleaseReservationAsync(
            pharmacyId,
            branchId,
            medicineId,
            request,
            GetActorUserId(),
            ct);

        return Ok(item);
    }

    [HttpPost("{medicineId:guid}/confirm")]
    public async Task<ActionResult<InventoryItemDto>> ConfirmReservation(
        [FromRoute] Guid pharmacyId,
        [FromRoute] Guid branchId,
        [FromRoute] Guid medicineId,
        [FromBody] ReservationRequest request,
        CancellationToken ct)
    {
        if (!await AuthorizeAccessAsync(pharmacyId))
            return Forbid();

        var item = await _inventoryService.ConfirmReservationAsync(
            pharmacyId,
            branchId,
            medicineId,
            request,
            GetActorUserId(),
            ct);

        return Ok(item);
    }

    [HttpGet("{medicineId:guid}/transactions")]
    public async Task<ActionResult<IReadOnlyList<InventoryTransactionDto>>> GetTransactions(
        [FromRoute] Guid pharmacyId,
        [FromRoute] Guid branchId,
        [FromRoute] Guid medicineId,
        CancellationToken ct)
    {
        if (!await AuthorizeAccessAsync(pharmacyId))
            return Forbid();

        var txs = await _inventoryService.GetTransactionsAsync(pharmacyId, branchId, medicineId, ct);
        return Ok(txs);
    }

    [HttpGet("{medicineId:guid}/price-history")]
    public async Task<ActionResult<IReadOnlyList<PriceHistoryDto>>> GetPriceHistory(
        [FromRoute] Guid pharmacyId,
        [FromRoute] Guid branchId,
        [FromRoute] Guid medicineId,
        CancellationToken ct)
    {
        if (!await AuthorizeAccessAsync(pharmacyId))
            return Forbid();

        var history = await _inventoryService.GetPriceHistoryAsync(pharmacyId, branchId, medicineId, ct);
        return Ok(history);
    }
}
