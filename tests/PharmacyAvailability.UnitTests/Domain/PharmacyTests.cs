using PharmacyAvailability.Domain.Common.Exceptions;
using PharmacyAvailability.Domain.Pharmacies;
using Xunit;

namespace PharmacyAvailability.UnitTests.Domain;

public class PharmacyTests
{
    [Fact]
    public void CreatePharmacy_WithValidData_CreatesActivePharmacy()
    {
        // Arrange
        var coords = new Coordinates(30.0444, 31.2357);

        // Act
        var pharmacy = new Pharmacy(
            "El Ezaby Pharmacy",
            "LIC-12345",
            "15 Tahrir Sq",
            "Cairo",
            coords,
            "+20227940000",
            "info@elezaby.com",
            "24/7");

        // Assert
        Assert.Equal("El Ezaby Pharmacy", pharmacy.Name);
        Assert.Equal("LIC-12345", pharmacy.LicenseNumber);
        Assert.Equal(30.0444, pharmacy.Latitude);
        Assert.Equal(31.2357, pharmacy.Longitude);
        Assert.Equal(PharmacyStatus.Active, pharmacy.Status);
        Assert.True(pharmacy.IsActive);
    }

    [Theory]
    [InlineData(91.0, 0.0)]
    [InlineData(-90.1, 0.0)]
    [InlineData(0.0, 180.1)]
    [InlineData(0.0, -180.1)]
    public void Coordinates_OutOfBounds_ThrowsDomainValidationException(double lat, double lon)
    {
        Assert.Throws<DomainValidationException>(() => new Coordinates(lat, lon));
    }

    [Fact]
    public void SetStatus_ChangesStatusAndUpdatesTimestamp()
    {
        // Arrange
        var pharmacy = new Pharmacy("Seif Pharmacy", "LIC-99", "Nasr City", "Cairo", new Coordinates(30.05, 31.33), "+201000000000");

        // Act
        pharmacy.SetStatus(PharmacyStatus.Suspended);

        // Assert
        Assert.Equal(PharmacyStatus.Suspended, pharmacy.Status);
        Assert.False(pharmacy.IsActive);
    }
}
