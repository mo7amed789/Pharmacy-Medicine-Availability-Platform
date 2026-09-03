using PharmacyAvailability.Domain.Common.Exceptions;
using PharmacyAvailability.Domain.Inventory;
using Xunit;

namespace PharmacyAvailability.UnitTests.Inventory;

public class InventoryDomainTests
{
    private readonly Guid _pharmacyId = Guid.NewGuid();
    private readonly Guid _branchId = Guid.NewGuid();
    private readonly Guid _medicineId = Guid.NewGuid();
    private readonly Guid _actorUserId = Guid.NewGuid();

    [Fact]
    public void CreateInventory_WithValidData_InitializesCorrectly()
    {
        var inventory = new PharmacyInventory(
            _pharmacyId,
            _branchId,
            _medicineId,
            50,
            120.50m,
            "EGP",
            InventorySource.Manual);

        Assert.Equal(_pharmacyId, inventory.PharmacyId);
        Assert.Equal(_branchId, inventory.BranchId);
        Assert.Equal(_medicineId, inventory.MedicineId);
        Assert.Equal(50, inventory.AvailableQuantity);
        Assert.Equal(0, inventory.ReservedQuantity);
        Assert.Equal(120.50m, inventory.Price);
        Assert.Equal("EGP", inventory.Currency);
        Assert.Equal(InventorySource.Manual, inventory.Source);
    }

    [Fact]
    public void CreateInventory_EmptyBranchId_ThrowsDomainValidationException()
    {
        Assert.Throws<DomainValidationException>(() =>
            new PharmacyInventory(_pharmacyId, Guid.Empty, _medicineId, 10, 50m));
    }

    [Fact]
    public void CreateInventory_NegativeQuantity_ThrowsDomainValidationException()
    {
        Assert.Throws<DomainValidationException>(() =>
            new PharmacyInventory(_pharmacyId, _branchId, _medicineId, -1, 50m));
    }

    [Fact]
    public void CreateInventory_NegativePrice_ThrowsInvalidPriceException()
    {
        Assert.Throws<InvalidPriceException>(() =>
            new PharmacyInventory(_pharmacyId, _branchId, _medicineId, 10, -5m));
    }

    [Fact]
    public void AdjustStock_PositiveRestock_IncreasesStockAndAddsTransaction()
    {
        var inventory = new PharmacyInventory(_pharmacyId, _branchId, _medicineId, 10, 50m);
        var tx = inventory.AdjustStock(5, InventoryAdjustmentReason.Restock, _actorUserId, "Restock arrived");

        Assert.Equal(15, inventory.AvailableQuantity);
        Assert.Single(inventory.Transactions);
        Assert.Equal(5, tx.DeltaQuantity);
        Assert.Equal(15, tx.ResultingQuantity);
        Assert.Equal(InventoryAdjustmentReason.Restock, tx.Reason);
        Assert.Equal(_actorUserId, tx.ActorUserId);
    }

    [Fact]
    public void AdjustStock_NegativeSale_DecreasesStockAndAddsTransaction()
    {
        var inventory = new PharmacyInventory(_pharmacyId, _branchId, _medicineId, 10, 50m);
        var tx = inventory.AdjustStock(-4, InventoryAdjustmentReason.Sale, _actorUserId, "Walk-in sale");

        Assert.Equal(6, inventory.AvailableQuantity);
        Assert.Single(inventory.Transactions);
        Assert.Equal(-4, tx.DeltaQuantity);
        Assert.Equal(6, tx.ResultingQuantity);
    }

    [Fact]
    public void AdjustStock_ZeroDelta_ThrowsDomainValidationException()
    {
        var inventory = new PharmacyInventory(_pharmacyId, _branchId, _medicineId, 10, 50m);
        Assert.Throws<DomainValidationException>(() =>
            inventory.AdjustStock(0, InventoryAdjustmentReason.ManualAdjustment, _actorUserId));
    }

    [Fact]
    public void AdjustStock_InsufficientStock_ThrowsInsufficientStockException()
    {
        var inventory = new PharmacyInventory(_pharmacyId, _branchId, _medicineId, 5, 50m);
        Assert.Throws<InsufficientStockException>(() =>
            inventory.AdjustStock(-10, InventoryAdjustmentReason.Sale, _actorUserId));
    }

    [Fact]
    public void Reserve_ValidQuantity_DecreasesAvailableAndIncreasesReserved()
    {
        var inventory = new PharmacyInventory(_pharmacyId, _branchId, _medicineId, 20, 50m);
        var tx = inventory.Reserve(5, _actorUserId, "Online order hold");

        Assert.Equal(15, inventory.AvailableQuantity);
        Assert.Equal(5, inventory.ReservedQuantity);
        Assert.Single(inventory.Transactions);
        Assert.Equal(-5, tx.DeltaQuantity);
        Assert.Equal(15, tx.ResultingQuantity);
        Assert.Equal(InventoryAdjustmentReason.ReservationHold, tx.Reason);
    }

    [Fact]
    public void Reserve_ExceedingAvailableStock_ThrowsInsufficientStockException()
    {
        var inventory = new PharmacyInventory(_pharmacyId, _branchId, _medicineId, 3, 50m);
        Assert.Throws<InsufficientStockException>(() =>
            inventory.Reserve(5, _actorUserId));
    }

    [Fact]
    public void ReleaseReservation_RestoresAvailableAndDecrementsReserved()
    {
        var inventory = new PharmacyInventory(_pharmacyId, _branchId, _medicineId, 20, 50m);
        inventory.Reserve(5, _actorUserId);

        var tx = inventory.ReleaseReservation(3, _actorUserId, "Partial order cancellation");

        Assert.Equal(18, inventory.AvailableQuantity);
        Assert.Equal(2, inventory.ReservedQuantity);
        Assert.Equal(2, inventory.Transactions.Count);
        Assert.Equal(3, tx.DeltaQuantity);
        Assert.Equal(18, tx.ResultingQuantity);
        Assert.Equal(InventoryAdjustmentReason.ReservationRelease, tx.Reason);
    }

    [Fact]
    public void ReleaseReservation_ExceedingReserved_ThrowsDomainValidationException()
    {
        var inventory = new PharmacyInventory(_pharmacyId, _branchId, _medicineId, 20, 50m);
        inventory.Reserve(5, _actorUserId);

        Assert.Throws<DomainValidationException>(() =>
            inventory.ReleaseReservation(10, _actorUserId));
    }

    [Fact]
    public void ConfirmReservation_DeductsFromReservedWithoutAffectingAvailable()
    {
        var inventory = new PharmacyInventory(_pharmacyId, _branchId, _medicineId, 20, 50m);
        inventory.Reserve(5, _actorUserId);

        var tx = inventory.ConfirmReservation(5, _actorUserId, "Customer picked up order");

        Assert.Equal(15, inventory.AvailableQuantity);
        Assert.Equal(0, inventory.ReservedQuantity);
        Assert.Equal(2, inventory.Transactions.Count);
        Assert.Equal(0, tx.DeltaQuantity);
        Assert.Equal(15, tx.ResultingQuantity);
        Assert.Equal(InventoryAdjustmentReason.ReservationConfirmed, tx.Reason);
    }

    [Fact]
    public void UpdatePrice_DifferentPrice_UpdatesPriceAndCreatesHistory()
    {
        var inventory = new PharmacyInventory(_pharmacyId, _branchId, _medicineId, 10, 50.00m);
        var history = inventory.UpdatePrice(59.99m, _actorUserId);

        Assert.NotNull(history);
        Assert.Equal(59.99m, inventory.Price);
        Assert.Equal(50.00m, history.OldPrice);
        Assert.Equal(59.99m, history.NewPrice);
        Assert.Equal(_actorUserId, history.ChangedByUserId);
        Assert.Single(inventory.PriceHistories);
    }

    [Fact]
    public void UpdatePrice_SamePrice_ReturnsNullAndDoesNotCreateHistory()
    {
        var inventory = new PharmacyInventory(_pharmacyId, _branchId, _medicineId, 10, 50.00m);
        var history = inventory.UpdatePrice(50.00m, _actorUserId);

        Assert.Null(history);
        Assert.Empty(inventory.PriceHistories);
    }

    [Fact]
    public void UpdatePrice_NegativePrice_ThrowsInvalidPriceException()
    {
        var inventory = new PharmacyInventory(_pharmacyId, _branchId, _medicineId, 10, 50.00m);
        Assert.Throws<InvalidPriceException>(() =>
            inventory.UpdatePrice(-10m, _actorUserId));
    }

    [Fact]
    public void Freshness_CalculatesAccurately()
    {
        var inventory = new PharmacyInventory(_pharmacyId, _branchId, _medicineId, 10, 50.00m);
        var now = DateTime.UtcNow;

        Assert.Equal(StockFreshness.Fresh, inventory.GetFreshness(now));
        Assert.Equal(StockFreshness.RecentlyUpdated, inventory.GetFreshness(now.AddHours(1)));
        Assert.Equal(StockFreshness.Stale, inventory.GetFreshness(now.AddHours(5)));
        Assert.Equal(StockFreshness.Outdated, inventory.GetFreshness(now.AddDays(2)));
    }
}
