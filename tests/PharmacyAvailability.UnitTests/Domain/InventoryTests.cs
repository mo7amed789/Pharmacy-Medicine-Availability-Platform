using PharmacyAvailability.Domain.Common.Exceptions;
using PharmacyAvailability.Domain.Inventory;
using Xunit;

namespace PharmacyAvailability.UnitTests.Domain;

public class InventoryTests
{
    [Fact]
    public void CreateInventory_WithValidData_InitializesCorrectly()
    {
        // Arrange
        var pharmacyId = Guid.NewGuid();
        var medicineId = Guid.NewGuid();

        // Act
        var inventory = new PharmacyInventory(pharmacyId, medicineId, 10, 85.50m, "EGP");

        // Assert
        Assert.Equal(pharmacyId, inventory.PharmacyId);
        Assert.Equal(medicineId, inventory.MedicineId);
        Assert.Equal(10, inventory.AvailableQuantity);
        Assert.Equal(85.50m, inventory.Price);
        Assert.Equal("EGP", inventory.Currency);
        Assert.Equal(InventorySource.Manual, inventory.Source);
        Assert.Empty(inventory.Transactions);
        Assert.Empty(inventory.PriceHistories);
    }

    [Fact]
    public void CreateInventory_WithNegativePrice_ThrowsInvalidPriceException()
    {
        // Arrange & Act & Assert
        Assert.Throws<InvalidPriceException>(() =>
            new PharmacyInventory(Guid.NewGuid(), Guid.NewGuid(), 10, -5.00m));
    }

    [Fact]
    public void CreateInventory_WithNegativeQuantity_ThrowsDomainValidationException()
    {
        // Arrange & Act & Assert
        Assert.Throws<DomainValidationException>(() =>
            new PharmacyInventory(Guid.NewGuid(), Guid.NewGuid(), -1, 50.00m));
    }

    [Fact]
    public void AdjustStock_PositiveDelta_IncreasesStockAndAddsTransaction()
    {
        // Arrange
        var inventory = new PharmacyInventory(Guid.NewGuid(), Guid.NewGuid(), 5, 100m);
        var actorId = Guid.NewGuid();

        // Act
        var tx = inventory.AdjustStock(3, InventoryAdjustmentReason.Restock, actorId, "Weekly shipment");

        // Assert
        Assert.Equal(8, inventory.AvailableQuantity);
        Assert.Single(inventory.Transactions);
        Assert.Equal(3, tx.DeltaQuantity);
        Assert.Equal(8, tx.ResultingQuantity);
        Assert.Equal(InventoryAdjustmentReason.Restock, tx.Reason);
        Assert.Equal(actorId, tx.ActorUserId);
        Assert.Equal("Weekly shipment", tx.Notes);
    }

    [Fact]
    public void AdjustStock_NegativeDeltaWithinAvailable_DecreasesStockAndAddsTransaction()
    {
        // Arrange
        var inventory = new PharmacyInventory(Guid.NewGuid(), Guid.NewGuid(), 5, 100m);
        var actorId = Guid.NewGuid();

        // Act
        var tx = inventory.AdjustStock(-2, InventoryAdjustmentReason.Sale, actorId, "Customer counter sale");

        // Assert
        Assert.Equal(3, inventory.AvailableQuantity);
        Assert.Single(inventory.Transactions);
        Assert.Equal(-2, tx.DeltaQuantity);
        Assert.Equal(3, tx.ResultingQuantity);
        Assert.Equal(InventoryAdjustmentReason.Sale, tx.Reason);
    }

    [Fact]
    public void AdjustStock_DeductMoreThanAvailable_ThrowsInsufficientStockException_DoesNotChangeState()
    {
        // Arrange
        var inventory = new PharmacyInventory(Guid.NewGuid(), Guid.NewGuid(), 2, 100m);
        var actorId = Guid.NewGuid();

        // Act & Assert
        var ex = Assert.Throws<InsufficientStockException>(() =>
            inventory.AdjustStock(-3, InventoryAdjustmentReason.Sale, actorId));

        Assert.Equal(2, ex.AvailableQuantity);
        Assert.Equal(-3, ex.RequestedDelta);
        Assert.Equal(2, inventory.AvailableQuantity);
        Assert.Empty(inventory.Transactions);
    }

    [Fact]
    public void AdjustStock_ZeroDelta_ThrowsDomainValidationException()
    {
        // Arrange
        var inventory = new PharmacyInventory(Guid.NewGuid(), Guid.NewGuid(), 5, 100m);

        // Act & Assert
        Assert.Throws<DomainValidationException>(() =>
            inventory.AdjustStock(0, InventoryAdjustmentReason.ManualAdjustment, Guid.NewGuid()));
    }

    [Fact]
    public void UpdatePrice_ValidNewPrice_UpdatesPriceAndRecordsHistory()
    {
        // Arrange
        var inventory = new PharmacyInventory(Guid.NewGuid(), Guid.NewGuid(), 10, 50.00m);
        var actorId = Guid.NewGuid();

        // Act
        var history = inventory.UpdatePrice(55.25m, actorId);

        // Assert
        Assert.NotNull(history);
        Assert.Equal(55.25m, inventory.Price);
        Assert.Single(inventory.PriceHistories);
        Assert.Equal(50.00m, history.OldPrice);
        Assert.Equal(55.25m, history.NewPrice);
        Assert.Equal(actorId, history.ChangedByUserId);
    }

    [Fact]
    public void UpdatePrice_SamePrice_ReturnsNullAndDoesNotRecordHistory()
    {
        // Arrange
        var inventory = new PharmacyInventory(Guid.NewGuid(), Guid.NewGuid(), 10, 50.00m);
        var actorId = Guid.NewGuid();

        // Act
        var history = inventory.UpdatePrice(50.00m, actorId);

        // Assert
        Assert.Null(history);
        Assert.Empty(inventory.PriceHistories);
    }

    [Fact]
    public void UpdatePrice_NegativePrice_ThrowsInvalidPriceException()
    {
        // Arrange
        var inventory = new PharmacyInventory(Guid.NewGuid(), Guid.NewGuid(), 10, 50.00m);

        // Act & Assert
        Assert.Throws<InvalidPriceException>(() => inventory.UpdatePrice(-10m, Guid.NewGuid()));
    }

    [Fact]
    public void UpdateFromSync_ValidData_UpdatesQuantityPriceSourceAndTimestamps()
    {
        // Arrange
        var inventory = new PharmacyInventory(Guid.NewGuid(), Guid.NewGuid(), 10, 50.00m);
        var syncTime = DateTime.UtcNow.AddMinutes(-5);

        // Act
        inventory.UpdateFromSync(25, 48.75m, syncTime);

        // Assert
        Assert.Equal(25, inventory.AvailableQuantity);
        Assert.Equal(48.75m, inventory.Price);
        Assert.Equal(InventorySource.IntegrationSync, inventory.Source);
        Assert.Equal(syncTime, inventory.LastSuccessfulSyncAt);
        Assert.Equal(syncTime, inventory.UpdatedAt);
    }

    [Theory]
    [InlineData(5, StockFreshness.Fresh)]          // 5 mins ago -> Fresh (< 15 mins)
    [InlineData(30, StockFreshness.RecentlyUpdated)] // 30 mins ago -> RecentlyUpdated (15m - 2h)
    [InlineData(180, StockFreshness.Stale)]        // 3 hours ago -> Stale (2h - 24h)
    [InlineData(1500, StockFreshness.Outdated)]     // 25 hours ago -> Outdated (> 24h)
    public void GetFreshness_CalculatesCorrectFreshnessTier(int minutesAgo, StockFreshness expectedTier)
    {
        // Arrange
        var inventory = new PharmacyInventory(Guid.NewGuid(), Guid.NewGuid(), 10, 50.00m);
        var now = DateTime.UtcNow;
        inventory.UpdateFromSync(10, 50.00m, now.AddMinutes(-minutesAgo));

        // Act
        var freshness = inventory.GetFreshness(now);

        // Assert
        Assert.Equal(expectedTier, freshness);
    }
}
