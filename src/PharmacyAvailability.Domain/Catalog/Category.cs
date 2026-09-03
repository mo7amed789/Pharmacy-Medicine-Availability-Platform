using PharmacyAvailability.Domain.Common;
using PharmacyAvailability.Domain.Common.Exceptions;

namespace PharmacyAvailability.Domain.Catalog;

public class Category : BaseEntity
{
    public string Name { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public bool IsActive { get; private set; } = true;

    // Required by EF Core
    protected Category() { }

    public Category(string name, string? description = null)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainValidationException("Category name is required.");

        Name = name.Trim();
        Description = description?.Trim();
        IsActive = true;
    }

    public void Update(string name, string? description)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainValidationException("Category name cannot be empty.");

        Name = name.Trim();
        Description = description?.Trim();
    }

    public void Deactivate() => IsActive = false;
    public void Activate() => IsActive = true;
}
