using PharmacyAvailability.Domain.Common;
using PharmacyAvailability.Domain.Common.Exceptions;

namespace PharmacyAvailability.Domain.Catalog;

public class MedicineBarcode : BaseEntity
{
    public Guid MedicineId { get; private set; }
    public string Barcode { get; private set; } = string.Empty;
    public bool IsPrimary { get; private set; } = true;

    // Required by EF Core
    protected MedicineBarcode() { }

    public MedicineBarcode(Guid medicineId, string barcode, bool isPrimary = true)
    {
        if (medicineId == Guid.Empty)
            throw new DomainValidationException("MedicineId is required.");
        if (string.IsNullOrWhiteSpace(barcode))
            throw new DomainValidationException("Barcode is required.");

        MedicineId = medicineId;
        Barcode = barcode.Trim();
        IsPrimary = isPrimary;
    }

    public void SetPrimary(bool isPrimary) => IsPrimary = isPrimary;
}
