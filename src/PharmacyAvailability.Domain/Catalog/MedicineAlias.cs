using PharmacyAvailability.Domain.Common;
using PharmacyAvailability.Domain.Common.Exceptions;

namespace PharmacyAvailability.Domain.Catalog;

public class MedicineAlias : BaseEntity
{
    public Guid MedicineId { get; private set; }
    public string Alias { get; private set; } = string.Empty;
    public string? LanguageCode { get; private set; }

    // Required by EF Core
    protected MedicineAlias() { }

    public MedicineAlias(Guid medicineId, string alias, string? languageCode = null)
    {
        if (medicineId == Guid.Empty)
            throw new DomainValidationException("MedicineId is required.");
        if (string.IsNullOrWhiteSpace(alias))
            throw new DomainValidationException("Alias is required.");

        MedicineId = medicineId;
        Alias = alias.Trim();
        LanguageCode = languageCode?.Trim().ToLowerInvariant();
    }
}
