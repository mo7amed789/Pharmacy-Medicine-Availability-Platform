namespace PharmacyAvailability.Domain.Inventory;

public enum StockFreshness
{
    Fresh = 1,           // e.g. < 15 minutes
    RecentlyUpdated = 2, // e.g. 15m - 2h
    Stale = 3,           // e.g. 2h - 24h
    Outdated = 4         // e.g. > 24h
}
