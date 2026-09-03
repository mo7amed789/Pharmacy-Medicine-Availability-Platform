using PharmacyAvailability.Domain.Common;
using PharmacyAvailability.Domain.Common.Exceptions;

namespace PharmacyAvailability.Domain.Integration;

public class InventorySyncLog : BaseEntity
{
    public Guid PharmacyId { get; private set; }
    public string SyncBatchId { get; private set; } = string.Empty;
    public DateTime StartedAt { get; private set; }
    public DateTime? CompletedAt { get; private set; }
    public bool Success { get; private set; }
    public int ItemsProcessed { get; private set; }
    public int ItemsUpdated { get; private set; }
    public int ItemsFailed { get; private set; }
    public string? ErrorMessage { get; private set; }

    // Required by EF Core
    protected InventorySyncLog() { }

    public InventorySyncLog(Guid pharmacyId, string syncBatchId)
    {
        if (pharmacyId == Guid.Empty)
            throw new DomainValidationException("PharmacyId is required.");
        if (string.IsNullOrWhiteSpace(syncBatchId))
            throw new DomainValidationException("SyncBatchId is required.");

        PharmacyId = pharmacyId;
        SyncBatchId = syncBatchId.Trim();
        StartedAt = DateTime.UtcNow;
    }

    public void MarkCompleted(int itemsProcessed, int itemsUpdated, int itemsFailed, string? errorMessage = null)
    {
        CompletedAt = DateTime.UtcNow;
        ItemsProcessed = itemsProcessed;
        ItemsUpdated = itemsUpdated;
        ItemsFailed = itemsFailed;
        Success = itemsFailed == 0 && string.IsNullOrWhiteSpace(errorMessage);
        ErrorMessage = errorMessage?.Trim();
    }
}
