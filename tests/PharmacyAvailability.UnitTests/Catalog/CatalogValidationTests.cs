using PharmacyAvailability.Domain.Catalog;
using PharmacyAvailability.Domain.Common.Exceptions;
using Xunit;

namespace PharmacyAvailability.UnitTests.Catalog;

public class CatalogValidationTests
{
    private readonly Guid _validCategoryId = Guid.NewGuid();
    private readonly Guid _validManufacturerId = Guid.NewGuid();

    [Theory]
    [InlineData("", "Generic", "Active", "10mg", "Tablet")]
    [InlineData("   ", "Generic", "Active", "10mg", "Tablet")]
    [InlineData("Brand", "", "Active", "10mg", "Tablet")]
    [InlineData("Brand", "Generic", "", "10mg", "Tablet")]
    [InlineData("Brand", "Generic", "Active", "", "Tablet")]
    [InlineData("Brand", "Generic", "Active", "10mg", "")]
    public void Medicine_Constructor_EmptyRequiredFields_ThrowsDomainValidationException(
        string brand, string generic, string active, string strength, string dosage)
    {
        Assert.Throws<DomainValidationException>(() =>
            new Medicine(brand, generic, active, strength, dosage, _validCategoryId, _validManufacturerId));
    }

    [Fact]
    public void Medicine_Constructor_EmptyCategoryId_ThrowsDomainValidationException()
    {
        Assert.Throws<DomainValidationException>(() =>
            new Medicine("Panadol", "Paracetamol", "Paracetamol", "500mg", "Tablet", Guid.Empty, _validManufacturerId));
    }

    [Fact]
    public void Medicine_Constructor_EmptyManufacturerId_ThrowsDomainValidationException()
    {
        Assert.Throws<DomainValidationException>(() =>
            new Medicine("Panadol", "Paracetamol", "Paracetamol", "500mg", "Tablet", _validCategoryId, Guid.Empty));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Medicine_AddBarcode_EmptyBarcode_ThrowsDomainValidationException(string barcode)
    {
        var medicine = new Medicine("Brand", "Generic", "Active", "10mg", "Tablet", _validCategoryId, _validManufacturerId);

        Assert.Throws<DomainValidationException>(() => medicine.AddBarcode(barcode));
    }

    [Fact]
    public void Medicine_AddBarcode_DuplicateBarcodeOnSameMedicine_ThrowsDomainValidationException()
    {
        var medicine = new Medicine("Brand", "Generic", "Active", "10mg", "Tablet", _validCategoryId, _validManufacturerId);
        medicine.AddBarcode("1234567890123");

        Assert.Throws<DomainValidationException>(() => medicine.AddBarcode("1234567890123"));
    }

    [Fact]
    public void Medicine_AddBarcode_CaseInsensitiveDuplicateOnSameMedicine_ThrowsDomainValidationException()
    {
        var medicine = new Medicine("Brand", "Generic", "Active", "10mg", "Tablet", _validCategoryId, _validManufacturerId);
        medicine.AddBarcode("ABC123XYZ");

        Assert.Throws<DomainValidationException>(() => medicine.AddBarcode("abc123xyz"));
    }

    [Fact]
    public void Medicine_AddBarcode_FirstBarcodeBecomesPrimaryByDefault()
    {
        var medicine = new Medicine("Brand", "Generic", "Active", "10mg", "Tablet", _validCategoryId, _validManufacturerId);
        var barcode = medicine.AddBarcode("1111111111111");

        Assert.True(barcode.IsPrimary);
    }

    [Fact]
    public void Medicine_AddBarcode_NewPrimaryBarcode_DemotesPreviousPrimary()
    {
        var medicine = new Medicine("Brand", "Generic", "Active", "10mg", "Tablet", _validCategoryId, _validManufacturerId);
        var first = medicine.AddBarcode("1111111111111", isPrimary: true);
        var second = medicine.AddBarcode("2222222222222", isPrimary: true);

        Assert.False(first.IsPrimary);
        Assert.True(second.IsPrimary);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Medicine_AddAlias_EmptyAlias_ThrowsDomainValidationException(string alias)
    {
        var medicine = new Medicine("Brand", "Generic", "Active", "10mg", "Tablet", _validCategoryId, _validManufacturerId);

        Assert.Throws<DomainValidationException>(() => medicine.AddAlias(alias));
    }

    [Fact]
    public void Medicine_AddAlias_DuplicateAliasOnSameMedicine_ThrowsDomainValidationException()
    {
        var medicine = new Medicine("Brand", "Generic", "Active", "10mg", "Tablet", _validCategoryId, _validManufacturerId);
        medicine.AddAlias("Panadol Extra Red");

        Assert.Throws<DomainValidationException>(() => medicine.AddAlias("panadol extra red"));
    }

    [Fact]
    public void Category_Constructor_EmptyName_ThrowsDomainValidationException()
    {
        Assert.Throws<DomainValidationException>(() => new Category("", "Description"));
    }

    [Fact]
    public void Manufacturer_Constructor_EmptyName_ThrowsDomainValidationException()
    {
        Assert.Throws<DomainValidationException>(() => new Manufacturer("", "Egypt"));
    }

    [Fact]
    public void Medicine_DeactivateAndActivate_TransitionsStateCorrectly()
    {
        var medicine = new Medicine("Brand", "Generic", "Active", "10mg", "Tablet", _validCategoryId, _validManufacturerId);
        Assert.True(medicine.IsActive);

        medicine.Deactivate();
        Assert.False(medicine.IsActive);

        medicine.Activate();
        Assert.True(medicine.IsActive);
    }
}
