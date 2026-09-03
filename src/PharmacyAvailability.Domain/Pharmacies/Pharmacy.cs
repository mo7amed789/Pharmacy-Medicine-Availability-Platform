using PharmacyAvailability.Domain.Common;
using PharmacyAvailability.Domain.Common.Exceptions;

namespace PharmacyAvailability.Domain.Pharmacies;

public class Pharmacy : BaseEntity
{
    public string Name { get; private set; } = string.Empty;
    public string LicenseNumber { get; private set; } = string.Empty;
    public string Address { get; private set; } = string.Empty;
    public string City { get; private set; } = string.Empty;
    public double Latitude { get; private set; }
    public double Longitude { get; private set; }
    public string Phone { get; private set; } = string.Empty;
    public string? Email { get; private set; }
    public string? OpeningHours { get; private set; }
    public PharmacyStatus Status { get; private set; } = PharmacyStatus.Pending;
    public DateTime UpdatedAt { get; private set; } = DateTime.UtcNow;

    public bool IsActive => Status == PharmacyStatus.Active;

    // Required by EF Core
    protected Pharmacy() { }

    public Pharmacy(
        string name,
        string licenseNumber,
        string address,
        string city,
        Coordinates coordinates,
        string phone,
        string? email = null,
        string? openingHours = null)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainValidationException("Pharmacy name is required.");
        if (string.IsNullOrWhiteSpace(licenseNumber))
            throw new DomainValidationException("License number is required.");
        if (string.IsNullOrWhiteSpace(address))
            throw new DomainValidationException("Address is required.");
        if (string.IsNullOrWhiteSpace(city))
            throw new DomainValidationException("City is required.");
        if (string.IsNullOrWhiteSpace(phone))
            throw new DomainValidationException("Phone is required.");
        ArgumentNullException.ThrowIfNull(coordinates);

        Name = name.Trim();
        LicenseNumber = licenseNumber.Trim().ToUpperInvariant();
        Address = address.Trim();
        City = city.Trim();
        Latitude = coordinates.Latitude;
        Longitude = coordinates.Longitude;
        Phone = phone.Trim();
        Email = email?.Trim().ToLowerInvariant();
        OpeningHours = openingHours?.Trim();
        Status = PharmacyStatus.Active;
        UpdatedAt = DateTime.UtcNow;
    }

    public void UpdateProfile(string name, string address, string city, string phone, string? email, string? openingHours)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainValidationException("Pharmacy name cannot be empty.");
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
        Email = email?.Trim().ToLowerInvariant();
        OpeningHours = openingHours?.Trim();
        UpdatedAt = DateTime.UtcNow;
    }

    public void UpdateLocation(Coordinates coordinates)
    {
        ArgumentNullException.ThrowIfNull(coordinates);
        Latitude = coordinates.Latitude;
        Longitude = coordinates.Longitude;
        UpdatedAt = DateTime.UtcNow;
    }

    public void SetStatus(PharmacyStatus newStatus)
    {
        Status = newStatus;
        UpdatedAt = DateTime.UtcNow;
    }
}
