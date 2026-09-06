using FluentValidation;
using PharmacyAvailability.Contracts.Inventory;

namespace PharmacyAvailability.Application.Validation;

public class CreateInventoryItemRequestValidator : AbstractValidator<CreateInventoryItemRequest>
{
    public CreateInventoryItemRequestValidator()
    {
        RuleFor(x => x.MedicineId)
            .NotEmpty().WithMessage("MedicineId is required.");

        RuleFor(x => x.InitialQuantity)
            .GreaterThanOrEqualTo(0).WithMessage("Initial quantity cannot be negative.");

        RuleFor(x => x.Price)
            .GreaterThanOrEqualTo(0m).WithMessage("Price cannot be negative.");

        RuleFor(x => x.Currency)
            .NotEmpty().WithMessage("Currency is required.")
            .Length(3).WithMessage("Currency must be a 3-letter ISO code (e.g., EGP).");
    }
}

public class AdjustStockRequestValidator : AbstractValidator<AdjustStockRequest>
{
    public AdjustStockRequestValidator()
    {
        RuleFor(x => x.DeltaQuantity)
            .NotEqual(0).WithMessage("Adjustment delta cannot be zero.");

        RuleFor(x => x.Reason)
            .NotEmpty().WithMessage("Adjustment reason is required.")
            .MaximumLength(100).WithMessage("Reason must not exceed 100 characters.");

        RuleFor(x => x.Notes)
            .MaximumLength(500).WithMessage("Notes must not exceed 500 characters.")
            .When(x => !string.IsNullOrEmpty(x.Notes));
    }
}

public class UpdatePriceRequestValidator : AbstractValidator<UpdatePriceRequest>
{
    public UpdatePriceRequestValidator()
    {
        RuleFor(x => x.NewPrice)
            .GreaterThanOrEqualTo(0m).WithMessage("Price cannot be negative.");
    }
}
