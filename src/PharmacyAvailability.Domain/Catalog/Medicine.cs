using PharmacyAvailability.Domain.Common;
using PharmacyAvailability.Domain.Common.Exceptions;

namespace PharmacyAvailability.Domain.Catalog;

public class Medicine : BaseEntity
{
    public string BrandName { get; private set; } = string.Empty;
    public string GenericName { get; private set; } = string.Empty;
    public string ActiveIngredient { get; private set; } = string.Empty;
    public string Strength { get; private set; } = string.Empty;
    public string DosageForm { get; private set; } = string.Empty;
    public Guid CategoryId { get; private set; }
    public Guid ManufacturerId { get; private set; }
    public string? Description { get; private set; }
    public bool RequiresPrescription { get; private set; }
    public bool IsActive { get; private set; } = true;
    public DateTime UpdatedAt { get; private set; } = DateTime.UtcNow;

    private readonly List<MedicineBarcode> _barcodes = new();
    public IReadOnlyCollection<MedicineBarcode> Barcodes => _barcodes.AsReadOnly();

    private readonly List<MedicineAlias> _aliases = new();
    public IReadOnlyCollection<MedicineAlias> Aliases => _aliases.AsReadOnly();

    // Required by EF Core
    protected Medicine() { }

    public Medicine(
        string brandName,
        string genericName,
        string activeIngredient,
        string strength,
        string dosageForm,
        Guid categoryId,
        Guid manufacturerId,
        string? description = null,
        bool requiresPrescription = false)
    {
        if (string.IsNullOrWhiteSpace(brandName))
            throw new DomainValidationException("Brand name is required.");
        if (string.IsNullOrWhiteSpace(genericName))
            throw new DomainValidationException("Generic name is required.");
        if (string.IsNullOrWhiteSpace(activeIngredient))
            throw new DomainValidationException("Active ingredient is required.");
        if (string.IsNullOrWhiteSpace(strength))
            throw new DomainValidationException("Strength is required.");
        if (string.IsNullOrWhiteSpace(dosageForm))
            throw new DomainValidationException("Dosage form is required.");
        if (categoryId == Guid.Empty)
            throw new DomainValidationException("CategoryId is required.");
        if (manufacturerId == Guid.Empty)
            throw new DomainValidationException("ManufacturerId is required.");

        BrandName = brandName.Trim();
        GenericName = genericName.Trim();
        ActiveIngredient = activeIngredient.Trim();
        Strength = strength.Trim();
        DosageForm = dosageForm.Trim();
        CategoryId = categoryId;
        ManufacturerId = manufacturerId;
        Description = description?.Trim();
        RequiresPrescription = requiresPrescription;
        IsActive = true;
        UpdatedAt = DateTime.UtcNow;
    }

    public void UpdateDetails(
        string brandName,
        string genericName,
        string activeIngredient,
        string strength,
        string dosageForm,
        Guid categoryId,
        Guid manufacturerId,
        string? description,
        bool requiresPrescription)
    {
        if (string.IsNullOrWhiteSpace(brandName))
            throw new DomainValidationException("Brand name cannot be empty.");
        if (string.IsNullOrWhiteSpace(genericName))
            throw new DomainValidationException("Generic name cannot be empty.");
        if (string.IsNullOrWhiteSpace(activeIngredient))
            throw new DomainValidationException("Active ingredient cannot be empty.");
        if (string.IsNullOrWhiteSpace(strength))
            throw new DomainValidationException("Strength cannot be empty.");
        if (string.IsNullOrWhiteSpace(dosageForm))
            throw new DomainValidationException("Dosage form cannot be empty.");
        if (categoryId == Guid.Empty)
            throw new DomainValidationException("CategoryId is required.");
        if (manufacturerId == Guid.Empty)
            throw new DomainValidationException("ManufacturerId is required.");

        BrandName = brandName.Trim();
        GenericName = genericName.Trim();
        ActiveIngredient = activeIngredient.Trim();
        Strength = strength.Trim();
        DosageForm = dosageForm.Trim();
        CategoryId = categoryId;
        ManufacturerId = manufacturerId;
        Description = description?.Trim();
        RequiresPrescription = requiresPrescription;
        UpdatedAt = DateTime.UtcNow;
    }

    public MedicineBarcode AddBarcode(string barcode, bool isPrimary = false)
    {
        if (string.IsNullOrWhiteSpace(barcode))
            throw new DomainValidationException("Barcode cannot be empty.");

        var clean = barcode.Trim();
        if (_barcodes.Any(b => b.Barcode.Equals(clean, StringComparison.OrdinalIgnoreCase)))
            throw new DomainValidationException($"Barcode '{clean}' already exists for this medicine.");

        if (isPrimary)
        {
            foreach (var b in _barcodes)
                b.SetPrimary(false);
        }
        else if (_barcodes.Count == 0)
        {
            isPrimary = true;
        }

        var entity = new MedicineBarcode(Id, clean, isPrimary);
        _barcodes.Add(entity);
        UpdatedAt = DateTime.UtcNow;
        return entity;
    }

    public MedicineAlias AddAlias(string alias, string? languageCode = null)
    {
        if (string.IsNullOrWhiteSpace(alias))
            throw new DomainValidationException("Alias cannot be empty.");

        var clean = alias.Trim();
        if (_aliases.Any(a => a.Alias.Equals(clean, StringComparison.OrdinalIgnoreCase)))
            throw new DomainValidationException($"Alias '{clean}' already exists for this medicine.");

        var entity = new MedicineAlias(Id, clean, languageCode);
        _aliases.Add(entity);
        UpdatedAt = DateTime.UtcNow;
        return entity;
    }

    public void Deactivate()
    {
        IsActive = false;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Activate()
    {
        IsActive = true;
        UpdatedAt = DateTime.UtcNow;
    }
}
