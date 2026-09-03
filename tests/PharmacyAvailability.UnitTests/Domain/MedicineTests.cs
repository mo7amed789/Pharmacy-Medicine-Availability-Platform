using PharmacyAvailability.Domain.Catalog;
using PharmacyAvailability.Domain.Common.Exceptions;
using Xunit;

namespace PharmacyAvailability.UnitTests.Domain;

public class MedicineTests
{
    [Fact]
    public void CreateMedicine_WithValidData_InitializesCorrectly()
    {
        // Arrange
        var catId = Guid.NewGuid();
        var manId = Guid.NewGuid();

        // Act
        var medicine = new Medicine(
            "Panadol Extra",
            "Paracetamol / Caffeine",
            "Paracetamol 500mg, Caffeine 65mg",
            "500mg/65mg",
            "Tablet",
            catId,
            manId,
            "Pain reliever and fever reducer",
            false);

        // Assert
        Assert.Equal("Panadol Extra", medicine.BrandName);
        Assert.Equal("Paracetamol / Caffeine", medicine.GenericName);
        Assert.True(medicine.IsActive);
        Assert.Empty(medicine.Barcodes);
        Assert.Empty(medicine.Aliases);
    }

    [Fact]
    public void AddBarcode_FirstBarcode_BecomesPrimary()
    {
        // Arrange
        var medicine = new Medicine("Panadol", "Paracetamol", "Paracetamol", "500mg", "Tablet", Guid.NewGuid(), Guid.NewGuid());

        // Act
        var barcode = medicine.AddBarcode("6221234567890");

        // Assert
        Assert.True(barcode.IsPrimary);
        Assert.Single(medicine.Barcodes);
    }

    [Fact]
    public void AddBarcode_DuplicateBarcode_ThrowsDomainValidationException()
    {
        // Arrange
        var medicine = new Medicine("Panadol", "Paracetamol", "Paracetamol", "500mg", "Tablet", Guid.NewGuid(), Guid.NewGuid());
        medicine.AddBarcode("6221234567890");

        // Act & Assert
        Assert.Throws<DomainValidationException>(() => medicine.AddBarcode("6221234567890"));
    }

    [Fact]
    public void AddAlias_AddsAlternativeName()
    {
        // Arrange
        var medicine = new Medicine("Panadol Extra", "Paracetamol", "Paracetamol", "500mg", "Tablet", Guid.NewGuid(), Guid.NewGuid());

        // Act
        var alias = medicine.AddAlias("بانادول اكسترا", "ar");

        // Assert
        Assert.Single(medicine.Aliases);
        Assert.Equal("بانادول اكسترا", alias.Alias);
        Assert.Equal("ar", alias.LanguageCode);
    }
}
