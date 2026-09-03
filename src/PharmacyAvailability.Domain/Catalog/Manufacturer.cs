using PharmacyAvailability.Domain.Common;
using PharmacyAvailability.Domain.Common.Exceptions;

namespace PharmacyAvailability.Domain.Catalog;

public class Manufacturer : BaseEntity
{
    public string Name { get; private set; } = string.Empty;
    public string? Country { get; private set; }
    public bool IsActive { get; private set; } = true;

    // Required by EF Core
    protected Manufacturer() { }

    public Manufacturer(string name, string? country = null)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainValidationException("Manufacturer name is required.");

        Name = name.Trim();
        Country = country?.Trim();
        IsActive = true;
    }

    public void Update(string name, string? country)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainValidationException("Manufacturer name cannot be empty.");

        Name = name.Trim();
        Country = country?.Trim();
    }

    public void Deactivate() => IsActive = false;
    public void Activate() => IsActive = true;
}
