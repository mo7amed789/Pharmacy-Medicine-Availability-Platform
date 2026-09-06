using FluentValidation.TestHelper;
using PharmacyAvailability.Application.Validation;
using PharmacyAvailability.Contracts.Inventory;
using PharmacyAvailability.Contracts.Reservations;
using Xunit;

namespace PharmacyAvailability.UnitTests.Validation;

public class ReservationAndInventoryValidatorsTests
{
    private readonly CreateReservationRequestValidator _createReservationValidator = new();
    private readonly AdminReleaseReservationRequestValidator _adminReleaseValidator = new();
    private readonly CreateInventoryItemRequestValidator _createInventoryValidator = new();
    private readonly AdjustStockRequestValidator _adjustStockValidator = new();
    private readonly UpdatePriceRequestValidator _updatePriceValidator = new();

    [Fact]
    public void CreateReservation_ValidData_Passes()
    {
        var model = new CreateReservationRequest(Guid.NewGuid(), Guid.NewGuid(), 5);
        var result = _createReservationValidator.TestValidate(model);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(101)]
    public void CreateReservation_InvalidQuantity_Fails(int quantity)
    {
        var model = new CreateReservationRequest(Guid.NewGuid(), Guid.NewGuid(), quantity);
        var result = _createReservationValidator.TestValidate(model);
        result.ShouldHaveValidationErrorFor(x => x.Quantity);
    }

    [Fact]
    public void CreateReservation_EmptyGuids_Fails()
    {
        var model = new CreateReservationRequest(Guid.Empty, Guid.Empty, 2);
        var result = _createReservationValidator.TestValidate(model);
        result.ShouldHaveValidationErrorFor(x => x.BranchId);
        result.ShouldHaveValidationErrorFor(x => x.MedicineId);
    }

    [Fact]
    public void AdminReleaseReservation_EmptyReason_Fails()
    {
        var model = new AdminReleaseReservationRequest("");
        var result = _adminReleaseValidator.TestValidate(model);
        result.ShouldHaveValidationErrorFor(x => x.Reason);
    }

    [Fact]
    public void AdjustStock_ZeroDelta_Fails()
    {
        var model = new AdjustStockRequest(0, "Manual");
        var result = _adjustStockValidator.TestValidate(model);
        result.ShouldHaveValidationErrorFor(x => x.DeltaQuantity);
    }

    [Fact]
    public void AdjustStock_ValidNonZeroDelta_Passes()
    {
        var model = new AdjustStockRequest(10, "Shipment received");
        var result = _adjustStockValidator.TestValidate(model);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void UpdatePrice_NegativePrice_Fails()
    {
        var model = new UpdatePriceRequest(-10m);
        var result = _updatePriceValidator.TestValidate(model);
        result.ShouldHaveValidationErrorFor(x => x.NewPrice);
    }

    [Fact]
    public void UpdatePrice_ZeroOrPositivePrice_Passes()
    {
        var model = new UpdatePriceRequest(45.50m);
        var result = _updatePriceValidator.TestValidate(model);
        result.ShouldNotHaveAnyValidationErrors();
    }
}
