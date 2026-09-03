using PharmacyAvailability.Domain.Common;
using PharmacyAvailability.Domain.Common.Exceptions;

namespace PharmacyAvailability.Domain.Auditing;

public class AuditLog : BaseEntity
{
    public Guid? ActorUserId { get; private set; }
    public string Action { get; private set; } = string.Empty;
    public string EntityName { get; private set; } = string.Empty;
    public string EntityId { get; private set; } = string.Empty;
    public DateTime Timestamp { get; private set; } = DateTime.UtcNow;
    public string? DetailsJson { get; private set; }
    public string? IpAddress { get; private set; }

    // Required by EF Core
    protected AuditLog() { }

    public AuditLog(
        string action,
        string entityName,
        string entityId,
        Guid? actorUserId = null,
        string? detailsJson = null,
        string? ipAddress = null)
    {
        if (string.IsNullOrWhiteSpace(action))
            throw new DomainValidationException("Action is required.");
        if (string.IsNullOrWhiteSpace(entityName))
            throw new DomainValidationException("EntityName is required.");
        if (string.IsNullOrWhiteSpace(entityId))
            throw new DomainValidationException("EntityId is required.");

        Action = action.Trim();
        EntityName = entityName.Trim();
        EntityId = entityId.Trim();
        ActorUserId = actorUserId;
        Timestamp = DateTime.UtcNow;
        DetailsJson = detailsJson;
        IpAddress = ipAddress?.Trim();
    }
}
