using PharmacyAvailability.Domain.Common;
using PharmacyAvailability.Domain.Common.Exceptions;

namespace PharmacyAvailability.Domain.Inventory;

public class PharmacyInventory : BaseEntity
{
    public Guid PharmacyId { get; private set; }
    public Guid MedicineId { get; private set; }
    public int AvailableQuantity { get; private set; }
    public int ReservedQuantity { get; private set; }
    public decimal Price { get; private set; }
    public string Currency { get; private set; } = "EGP";
    public InventorySource Source { get; private set; } = InventorySource.Manual;
    public DateTime UpdatedAt { get; private set; } = DateTime.UtcNow;
    public DateTime? LastSuccessfulSyncAt { get; private set; }
    public byte[] RowVersion { get; private set; } = Array.Empty<byte>();

    private readonly List<InventoryTransaction> _transactions = new();
    public IReadOnlyCollection<InventoryTransaction> Transactions => _transactions.AsReadOnly();

    private readonly List<PriceHistory> _priceHistories = new();
    public IReadOnlyCollection<PriceHistory> PriceHistories => _priceHistories.AsReadOnly();

    // Required by EF Core
    protected PharmacyInventory() { }

    public PharmacyInventory(
        Guid pharmacyId,
        Guid medicineId,
        int initialQuantity,
        decimal initialPrice,
        string currency = "EGP",
        InventorySource source = InventorySource.Manual)
    {
        if (pharmacyId == Guid.Empty)
            throw new DomainValidationException("PharmacyId is required.");
        if (medicineId == Guid.Empty)
            throw new DomainValidationException("MedicineId is required.");
        if (initialQuantity < 0)
            throw new DomainValidationException($"Initial quantity cannot be negative ({initialQuantity}).");
        if (initialPrice < 0)
            throw new InvalidPriceException(initialPrice);
        if (string.IsNullOrWhiteSpace(currency))
            throw new DomainValidationException("Currency is required.");

        PharmacyId = pharmacyId;
        MedicineId = medicineId;
        AvailableQuantity = initialQuantity;
        ReservedQuantity = 0;
        Price = decimal.Round(initialPrice, 2);
        Currency = currency.Trim().ToUpperInvariant();
        Source = source;
        UpdatedAt = DateTime.UtcNow;
    }

    public InventoryTransaction AdjustStock(
        int delta,
        InventoryAdjustmentReason reason,
        Guid actorUserId,
        string? notes = null)
    {
        if (delta == 0)
            throw new DomainValidationException("Stock adjustment delta cannot be zero.");

        int newQuantity = AvailableQuantity + delta;
        if (newQuantity < 0)
            throw new InsufficientStockException(Id, AvailableQuantity, delta);

        AvailableQuantity = newQuantity;
        UpdatedAt = DateTime.UtcNow;

        var transaction = new InventoryTransaction(
            Id,
            delta,
            newQuantity,
            reason,
            actorUserId,
            notes);

        _transactions.Add(transaction);
        return transaction;
    }

    public PriceHistory? UpdatePrice(decimal newPrice, Guid actorUserId)
    {
        if (newPrice < 0)
            throw new InvalidPriceException(newPrice);

        decimal rounded = decimal.Round(newPrice, 2);
        if (Price == rounded)
            return null; // No price change needed

        var oldPrice = Price;
        Price = rounded;
        UpdatedAt = DateTime.UtcNow;

        var history = new PriceHistory(Id, oldPrice, rounded, actorUserId);
        _priceHistories.Add(history);
        return history;
    }

    public void UpdateFromSync(int newQuantity, decimal newPrice, DateTime syncTimestamp)
    {
        if (newQuantity < 0)
            throw new DomainValidationException($"Sync quantity cannot be negative ({newQuantity}).");
        if (newPrice < 0)
            throw new InvalidPriceException(newPrice);

        AvailableQuantity = newQuantity;
        Price = decimal.Round(newPrice, 2);
        Source = InventorySource.IntegrationSync;
        LastSuccessfulSyncAt = syncTimestamp;
        UpdatedAt = syncTimestamp;
    }

    public StockFreshness GetFreshness(
        DateTime currentUtc,
        TimeSpan? freshThreshold = null,
        TimeSpan? recentThreshold = null,
        TimeSpan? staleThreshold = null)
    {
        var freshLimit = freshThreshold ?? TimeSpan.FromMinutes(15);
        var recentLimit = recentThreshold ?? TimeSpan.FromHours(2);
        var staleLimit = staleThreshold ?? TimeSpan.FromHours(24);

        var elapsed = currentUtc - UpdatedAt;
        if (elapsed < TimeSpan.Zero)
            elapsed = TimeSpan.Zero; // handle clock skews gracefully

        if (elapsed <= freshLimit)
            return StockFreshness.Fresh;
        if (elapsed <= recentLimit)
            return StockFreshness.RecentlyUpdated;
        if (elapsed <= staleLimit)
            return StockFreshness.Stale;

        return StockFreshness.Outdated;
    }
}
