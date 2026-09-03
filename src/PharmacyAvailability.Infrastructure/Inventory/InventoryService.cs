using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using PharmacyAvailability.Application.Inventory;
using PharmacyAvailability.Contracts.Inventory;
using PharmacyAvailability.Domain.Common.Exceptions;
using PharmacyAvailability.Domain.Inventory;
using PharmacyAvailability.Domain.Pharmacies;
using PharmacyAvailability.Infrastructure.Persistence;

namespace PharmacyAvailability.Infrastructure.Inventory;

public class InventoryService : IInventoryService
{
    private readonly PharmacyDbContext _context;
    private readonly ILogger<InventoryService> _logger;

    public InventoryService(PharmacyDbContext context, ILogger<InventoryService> logger)
    {
        _context = context;
        _logger = logger;
    }

    private async Task<PharmacyBranch> ValidateBranchOwnershipAsync(Guid pharmacyId, Guid branchId, CancellationToken ct)
    {
        var pharmacy = await _context.Pharmacies
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == pharmacyId, ct);

        if (pharmacy == null)
            throw new KeyNotFoundException($"Pharmacy with ID '{pharmacyId}' was not found.");

        if (pharmacy.Status != PharmacyStatus.Active)
            throw new DomainValidationException($"Pharmacy is {pharmacy.Status} and cannot perform inventory operations.");

        var branch = await _context.PharmacyBranches
            .AsNoTracking()
            .FirstOrDefaultAsync(b => b.Id == branchId && b.PharmacyId == pharmacyId, ct);

        if (branch == null)
            throw new KeyNotFoundException($"Branch with ID '{branchId}' was not found for this pharmacy.");

        if (!branch.IsActive)
            throw new DomainValidationException($"Branch '{branch.Name}' is currently inactive and cannot perform inventory operations.");

        return branch;
    }

    private async Task ValidateMedicineAsync(Guid medicineId, CancellationToken ct)
    {
        var medicine = await _context.Medicines
            .AsNoTracking()
            .FirstOrDefaultAsync(m => m.Id == medicineId && m.IsActive, ct);

        if (medicine == null)
            throw new KeyNotFoundException($"Active medicine with ID '{medicineId}' was not found.");
    }

    public async Task<IReadOnlyList<InventoryItemDto>> GetBranchInventoryAsync(
        Guid pharmacyId,
        Guid branchId,
        CancellationToken ct = default)
    {
        await ValidateBranchOwnershipAsync(pharmacyId, branchId, ct);

        var items = await _context.PharmacyInventories
            .AsNoTracking()
            .Include(pi => pi.Medicine)
            .Where(pi => pi.PharmacyId == pharmacyId && pi.BranchId == branchId)
            .OrderBy(pi => pi.Medicine!.BrandName)
            .ToListAsync(ct);

        var now = DateTime.UtcNow;
        return items.Select(i => MapToDto(i, now)).ToList();
    }

    public async Task<InventoryItemDto?> GetInventoryItemByMedicineAsync(
        Guid pharmacyId,
        Guid branchId,
        Guid medicineId,
        CancellationToken ct = default)
    {
        await ValidateBranchOwnershipAsync(pharmacyId, branchId, ct);

        var item = await _context.PharmacyInventories
            .AsNoTracking()
            .Include(pi => pi.Medicine)
            .FirstOrDefaultAsync(pi => pi.PharmacyId == pharmacyId && pi.BranchId == branchId && pi.MedicineId == medicineId, ct);

        if (item == null)
            return null;

        return MapToDto(item, DateTime.UtcNow);
    }

    public async Task<InventoryItemDto> AddInventoryItemAsync(
        Guid pharmacyId,
        Guid branchId,
        CreateInventoryItemRequest request,
        Guid actorUserId,
        CancellationToken ct = default)
    {
        await ValidateBranchOwnershipAsync(pharmacyId, branchId, ct);
        await ValidateMedicineAsync(request.MedicineId, ct);

        var exists = await _context.PharmacyInventories
            .AnyAsync(pi => pi.BranchId == branchId && pi.MedicineId == request.MedicineId, ct);

        if (exists)
            throw new ResourceConflictException($"Medicine is already present in this branch's inventory.");

        var inventory = new PharmacyInventory(
            pharmacyId,
            branchId,
            request.MedicineId,
            0,
            request.Price,
            request.Currency,
            InventorySource.Manual);

        if (request.InitialQuantity > 0)
        {
            inventory.AdjustStock(
                request.InitialQuantity,
                InventoryAdjustmentReason.Restock,
                actorUserId,
                "Initial inventory stock");
        }

        _context.PharmacyInventories.Add(inventory);
        await _context.SaveChangesAsync(ct);

        _logger.LogInformation("Inventory item {InventoryId} created for branch {BranchId} (Medicine: {MedicineId})",
            inventory.Id, branchId, request.MedicineId);

        // Re-load with Medicine navigation
        var created = await _context.PharmacyInventories
            .AsNoTracking()
            .Include(pi => pi.Medicine)
            .FirstAsync(pi => pi.Id == inventory.Id, ct);

        return MapToDto(created, DateTime.UtcNow);
    }

    public async Task<InventoryItemDto> AdjustStockAsync(
        Guid pharmacyId,
        Guid branchId,
        Guid medicineId,
        AdjustStockRequest request,
        Guid actorUserId,
        CancellationToken ct = default)
    {
        await ValidateBranchOwnershipAsync(pharmacyId, branchId, ct);

        var inventory = await _context.PharmacyInventories
            .Include(pi => pi.Medicine)
            .FirstOrDefaultAsync(pi => pi.PharmacyId == pharmacyId && pi.BranchId == branchId && pi.MedicineId == medicineId, ct);

        if (inventory == null)
            throw new KeyNotFoundException($"Inventory item for medicine '{medicineId}' not found at this branch.");

        if (!Enum.TryParse<InventoryAdjustmentReason>(request.Reason, true, out var reason))
        {
            reason = InventoryAdjustmentReason.ManualAdjustment;
        }

        inventory.AdjustStock(request.DeltaQuantity, reason, actorUserId, request.Notes);
        await _context.SaveChangesAsync(ct);

        _logger.LogInformation("Stock adjusted for inventory {InventoryId}: Delta {Delta}, Result {Result}",
            inventory.Id, request.DeltaQuantity, inventory.AvailableQuantity);

        return MapToDto(inventory, DateTime.UtcNow);
    }

    public async Task<InventoryItemDto> UpdatePriceAsync(
        Guid pharmacyId,
        Guid branchId,
        Guid medicineId,
        UpdatePriceRequest request,
        Guid actorUserId,
        CancellationToken ct = default)
    {
        await ValidateBranchOwnershipAsync(pharmacyId, branchId, ct);

        var inventory = await _context.PharmacyInventories
            .Include(pi => pi.Medicine)
            .FirstOrDefaultAsync(pi => pi.PharmacyId == pharmacyId && pi.BranchId == branchId && pi.MedicineId == medicineId, ct);

        if (inventory == null)
            throw new KeyNotFoundException($"Inventory item for medicine '{medicineId}' not found at this branch.");

        inventory.UpdatePrice(request.NewPrice, actorUserId);
        await _context.SaveChangesAsync(ct);

        _logger.LogInformation("Price updated for inventory {InventoryId} to {NewPrice}",
            inventory.Id, request.NewPrice);

        return MapToDto(inventory, DateTime.UtcNow);
    }

    public async Task<InventoryItemDto> ReserveStockAsync(
        Guid pharmacyId,
        Guid branchId,
        Guid medicineId,
        ReservationRequest request,
        Guid actorUserId,
        CancellationToken ct = default)
    {
        await ValidateBranchOwnershipAsync(pharmacyId, branchId, ct);

        var inventory = await _context.PharmacyInventories
            .Include(pi => pi.Medicine)
            .FirstOrDefaultAsync(pi => pi.PharmacyId == pharmacyId && pi.BranchId == branchId && pi.MedicineId == medicineId, ct);

        if (inventory == null)
            throw new KeyNotFoundException($"Inventory item for medicine '{medicineId}' not found at this branch.");

        inventory.Reserve(request.Quantity, actorUserId, request.Notes);
        await _context.SaveChangesAsync(ct);

        _logger.LogInformation("Stock reserved for inventory {InventoryId}: Qty {Qty}, ReservedTotal {Reserved}",
            inventory.Id, request.Quantity, inventory.ReservedQuantity);

        return MapToDto(inventory, DateTime.UtcNow);
    }

    public async Task<InventoryItemDto> ReleaseReservationAsync(
        Guid pharmacyId,
        Guid branchId,
        Guid medicineId,
        ReservationRequest request,
        Guid actorUserId,
        CancellationToken ct = default)
    {
        await ValidateBranchOwnershipAsync(pharmacyId, branchId, ct);

        var inventory = await _context.PharmacyInventories
            .Include(pi => pi.Medicine)
            .FirstOrDefaultAsync(pi => pi.PharmacyId == pharmacyId && pi.BranchId == branchId && pi.MedicineId == medicineId, ct);

        if (inventory == null)
            throw new KeyNotFoundException($"Inventory item for medicine '{medicineId}' not found at this branch.");

        inventory.ReleaseReservation(request.Quantity, actorUserId, request.Notes);
        await _context.SaveChangesAsync(ct);

        _logger.LogInformation("Reservation released for inventory {InventoryId}: Qty {Qty}, AvailableTotal {Available}",
            inventory.Id, request.Quantity, inventory.AvailableQuantity);

        return MapToDto(inventory, DateTime.UtcNow);
    }

    public async Task<InventoryItemDto> ConfirmReservationAsync(
        Guid pharmacyId,
        Guid branchId,
        Guid medicineId,
        ReservationRequest request,
        Guid actorUserId,
        CancellationToken ct = default)
    {
        await ValidateBranchOwnershipAsync(pharmacyId, branchId, ct);

        var inventory = await _context.PharmacyInventories
            .Include(pi => pi.Medicine)
            .FirstOrDefaultAsync(pi => pi.PharmacyId == pharmacyId && pi.BranchId == branchId && pi.MedicineId == medicineId, ct);

        if (inventory == null)
            throw new KeyNotFoundException($"Inventory item for medicine '{medicineId}' not found at this branch.");

        inventory.ConfirmReservation(request.Quantity, actorUserId, request.Notes);
        await _context.SaveChangesAsync(ct);

        _logger.LogInformation("Reservation confirmed for inventory {InventoryId}: Qty {Qty}, RemainingReserved {Reserved}",
            inventory.Id, request.Quantity, inventory.ReservedQuantity);

        return MapToDto(inventory, DateTime.UtcNow);
    }

    public async Task<IReadOnlyList<InventoryTransactionDto>> GetTransactionsAsync(
        Guid pharmacyId,
        Guid branchId,
        Guid medicineId,
        CancellationToken ct = default)
    {
        await ValidateBranchOwnershipAsync(pharmacyId, branchId, ct);

        var inventory = await _context.PharmacyInventories
            .AsNoTracking()
            .FirstOrDefaultAsync(pi => pi.PharmacyId == pharmacyId && pi.BranchId == branchId && pi.MedicineId == medicineId, ct);

        if (inventory == null)
            throw new KeyNotFoundException($"Inventory item for medicine '{medicineId}' not found at this branch.");

        var txs = await _context.InventoryTransactions
            .AsNoTracking()
            .Where(t => t.PharmacyInventoryId == inventory.Id)
            .OrderByDescending(t => t.CreatedAt)
            .ToListAsync(ct);

        return txs.Select(t => new InventoryTransactionDto(
            t.Id,
            t.PharmacyInventoryId,
            t.DeltaQuantity,
            t.ResultingQuantity,
            t.Reason.ToString(),
            t.ActorUserId,
            t.Notes,
            t.CreatedAt)).ToList();
    }

    public async Task<IReadOnlyList<PriceHistoryDto>> GetPriceHistoryAsync(
        Guid pharmacyId,
        Guid branchId,
        Guid medicineId,
        CancellationToken ct = default)
    {
        await ValidateBranchOwnershipAsync(pharmacyId, branchId, ct);

        var inventory = await _context.PharmacyInventories
            .AsNoTracking()
            .FirstOrDefaultAsync(pi => pi.PharmacyId == pharmacyId && pi.BranchId == branchId && pi.MedicineId == medicineId, ct);

        if (inventory == null)
            throw new KeyNotFoundException($"Inventory item for medicine '{medicineId}' not found at this branch.");

        var histories = await _context.PriceHistories
            .AsNoTracking()
            .Where(ph => ph.PharmacyInventoryId == inventory.Id)
            .OrderByDescending(ph => ph.CreatedAt)
            .ToListAsync(ct);

        return histories.Select(h => new PriceHistoryDto(
            h.Id,
            h.PharmacyInventoryId,
            h.OldPrice,
            h.NewPrice,
            h.ChangedByUserId,
            h.CreatedAt)).ToList();
    }

    private static InventoryItemDto MapToDto(PharmacyInventory i, DateTime now) =>
        new(
            i.Id,
            i.PharmacyId,
            i.BranchId,
            i.MedicineId,
            i.Medicine?.BrandName ?? string.Empty,
            i.Medicine?.GenericName ?? string.Empty,
            i.AvailableQuantity,
            i.ReservedQuantity,
            i.AvailableQuantity + i.ReservedQuantity,
            i.Price,
            i.Currency,
            i.Source.ToString(),
            i.GetFreshness(now).ToString(),
            i.UpdatedAt);
}
