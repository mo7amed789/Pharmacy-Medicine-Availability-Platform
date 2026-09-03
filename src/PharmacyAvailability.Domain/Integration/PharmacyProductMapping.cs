using PharmacyAvailability.Domain.Common;
using PharmacyAvailability.Domain.Common.Exceptions;

namespace PharmacyAvailability.Domain.Integration;

public class PharmacyProductMapping : BaseEntity
{
    public Guid PharmacyId { get; private set; }
    public Guid MedicineId { get; private set; }
    public string ExternalProductId { get; private set; } = string.Empty;
    public string? ExternalBarcode { get; private set; }
    public string? ExternalName { get; private set; }
    public MappingStatus Status { get; private set; } = MappingStatus.PendingReview;
    public DateTime? LastSyncAt { get; private set; }

    // Required by EF Core
    protected PharmacyProductMapping() { }

    public PharmacyProductMapping(
        Guid pharmacyId,
        Guid medicineId,
        string externalProductId,
        string? externalBarcode = null,
        string? externalName = null,
        MappingStatus status = MappingStatus.Verified)
    {
        if (pharmacyId == Guid.Empty)
            throw new DomainValidationException("PharmacyId is required.");
        if (medicineId == Guid.Empty)
            throw new DomainValidationException("MedicineId is required.");
        if (string.IsNullOrWhiteSpace(externalProductId))
            throw new DomainValidationException("ExternalProductId is required.");

        PharmacyId = pharmacyId;
        MedicineId = medicineId;
        ExternalProductId = externalProductId.Trim();
        ExternalBarcode = externalBarcode?.Trim();
        ExternalName = externalName?.Trim();
        Status = status;
    }

    public void Verify() => Status = MappingStatus.Verified;
    public void Reject() => Status = MappingStatus.Rejected;
    public void RecordSync() => LastSyncAt = DateTime.UtcNow;
}
