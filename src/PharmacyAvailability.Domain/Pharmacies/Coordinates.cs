using PharmacyAvailability.Domain.Common.Exceptions;

namespace PharmacyAvailability.Domain.Pharmacies;

public record Coordinates
{
    public double Latitude { get; }
    public double Longitude { get; }

    public Coordinates(double latitude, double longitude)
    {
        if (latitude is < -90.0 or > 90.0)
            throw new DomainValidationException($"Latitude must be between -90 and +90 degrees. Supplied: {latitude}");
        if (longitude is < -180.0 or > 180.0)
            throw new DomainValidationException($"Longitude must be between -180 and +180 degrees. Supplied: {longitude}");

        Latitude = latitude;
        Longitude = longitude;
    }
}
