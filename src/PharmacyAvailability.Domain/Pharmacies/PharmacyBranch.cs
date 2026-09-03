using PharmacyAvailability.Domain.Common;
using PharmacyAvailability.Domain.Common.Exceptions;

namespace PharmacyAvailability.Domain.Pharmacies;

public class PharmacyBranch : BaseEntity
{
    public Guid PharmacyId { get; private set; }
    public Pharmacy? Pharmacy { get; private set; }

    public string Name { get; private set; } = string.Empty;
    public string? BranchCode { get; private set; }
    public string Address { get; private set; } = string.Empty;
    public string City { get; private set; } = string.Empty;
    public double Latitude { get; private set; }
    public double Longitude { get; private set; }
    public string Phone { get; private set; } = string.Empty;
    public string? OpeningHours { get; private set; }
    public bool IsActive { get; private set; } = true;
    public DateTime UpdatedAt { get; private set; } = DateTime.UtcNow;

    // Required by EF Core
    protected PharmacyBranch() { }

    public PharmacyBranch(
        Guid pharmacyId,
        string name,
        string address,
        string city,
        Coordinates coordinates,
        string phone,
        string? openingHours = null,
        string? branchCode = null,
        bool isActive = true)
    {
        if (pharmacyId == Guid.Empty)
            throw new DomainValidationException("PharmacyId is required.");
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainValidationException("Branch name is required.");
        if (string.IsNullOrWhiteSpace(address))
            throw new DomainValidationException("Address is required.");
        if (string.IsNullOrWhiteSpace(city))
            throw new DomainValidationException("City is required.");
        if (string.IsNullOrWhiteSpace(phone))
            throw new DomainValidationException("Phone is required.");
        ArgumentNullException.ThrowIfNull(coordinates);

        PharmacyId = pharmacyId;
        Name = name.Trim();
        Address = address.Trim();
        City = city.Trim();
        Latitude = coordinates.Latitude;
        Longitude = coordinates.Longitude;
        Phone = phone.Trim();
        OpeningHours = openingHours?.Trim();
        BranchCode = branchCode?.Trim();
        IsActive = isActive;
        UpdatedAt = DateTime.UtcNow;
    }

    public void UpdateDetails(
        string name,
        string address,
        string city,
        string phone,
        string? openingHours,
        string? branchCode)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainValidationException("Branch name cannot be empty.");
        if (string.IsNullOrWhiteSpace(address))
            throw new DomainValidationException("Address cannot be empty.");
        if (string.IsNullOrWhiteSpace(city))
            throw new DomainValidationException("City cannot be empty.");
        if (string.IsNullOrWhiteSpace(phone))
            throw new DomainValidationException("Phone cannot be empty.");

        Name = name.Trim();
        Address = address.Trim();
        City = city.Trim();
        Phone = phone.Trim();
        OpeningHours = openingHours?.Trim();
        BranchCode = branchCode?.Trim();
        UpdatedAt = DateTime.UtcNow;
    }

    public void UpdateLocation(Coordinates coordinates)
    {
        ArgumentNullException.ThrowIfNull(coordinates);
        Latitude = coordinates.Latitude;
        Longitude = coordinates.Longitude;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Activate()
    {
        IsActive = true;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Deactivate()
    {
        IsActive = false;
        UpdatedAt = DateTime.UtcNow;
    }
}
