using FluentValidation.TestHelper;
using PharmacyAvailability.Application.Validation;
using PharmacyAvailability.Contracts.Catalog;
using PharmacyAvailability.Contracts.Pharmacies;
using Xunit;

namespace PharmacyAvailability.UnitTests.Validation;

public class CatalogAndPharmacyValidatorsTests
{
    private readonly CreateMedicineRequestValidator _createMedicineValidator = new();
    private readonly RegisterPharmacyRequestValidator _registerPharmacyValidator = new();
    private readonly CreateBranchRequestValidator _createBranchValidator = new();
    private readonly InviteStaffRequestValidator _inviteStaffValidator = new();

    [Fact]
    public void CreateMedicine_ValidData_Passes()
    {
        var model = new CreateMedicineRequest(
            "Panadol Extra",
            "Paracetamol + Caffeine",
            "Paracetamol, Caffeine",
            "500mg/65mg",
            "Tablet",
            Guid.NewGuid(),
            Guid.NewGuid(),
            false,
            "123456789");

        var result = _createMedicineValidator.TestValidate(model);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void CreateMedicine_EmptyFields_Fails()
    {
        var model = new CreateMedicineRequest("", "", "", "", "", Guid.Empty, Guid.Empty, false);
        var result = _createMedicineValidator.TestValidate(model);

        result.ShouldHaveValidationErrorFor(x => x.BrandName);
        result.ShouldHaveValidationErrorFor(x => x.GenericName);
        result.ShouldHaveValidationErrorFor(x => x.CategoryId);
        result.ShouldHaveValidationErrorFor(x => x.ManufacturerId);
    }

    [Fact]
    public void RegisterPharmacy_InvalidCoordinates_Fails()
    {
        var model = new RegisterPharmacyRequest(
            "Test Pharmacy",
            "LIC123",
            "Address",
            "Cairo",
            120.0, // Invalid latitude (> 90)
            200.0, // Invalid longitude (> 180)
            "+201012345678",
            "test@pharma.com",
            "9-5",
            "Admin Name",
            "admin@pharma.com",
            "Admin123!");

        var result = _registerPharmacyValidator.TestValidate(model);
        result.ShouldHaveValidationErrorFor(x => x.Latitude);
        result.ShouldHaveValidationErrorFor(x => x.Longitude);
    }

    [Fact]
    public void RegisterPharmacy_ValidData_Passes()
    {
        var model = new RegisterPharmacyRequest(
            "El-Ezaby",
            "LIC12345",
            "123 Tahrir St",
            "Cairo",
            30.0444,
            31.2357,
            "+201012345678",
            "contact@ezaby.com",
            "24/7",
            "Ahmed El-Ezaby",
            "admin@ezaby.com",
            "Password123!");

        var result = _registerPharmacyValidator.TestValidate(model);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData("PharmacyStaff", true)]
    [InlineData("PharmacyAdmin", true)]
    [InlineData("Customer", false)]
    [InlineData("Unknown", false)]
    public void InviteStaff_RoleValidation(string role, bool isValid)
    {
        var model = new InviteStaffRequest("staff@test.com", "Password123!", "Staff Name", "+201012345678", role);
        var result = _inviteStaffValidator.TestValidate(model);

        if (isValid)
            result.ShouldNotHaveAnyValidationErrors();
        else
            result.ShouldHaveValidationErrorFor(x => x.Role);
    }
}
