using FluentValidation;
using PharmacyAvailability.Contracts.Catalog;

namespace PharmacyAvailability.Application.Validation;

public class CreateMedicineRequestValidator : AbstractValidator<CreateMedicineRequest>
{
    public CreateMedicineRequestValidator()
    {
        RuleFor(x => x.BrandName)
            .NotEmpty().WithMessage("Brand name is required.")
            .MaximumLength(150).WithMessage("Brand name must not exceed 150 characters.");

        RuleFor(x => x.GenericName)
            .NotEmpty().WithMessage("Generic name is required.")
            .MaximumLength(150).WithMessage("Generic name must not exceed 150 characters.");

        RuleFor(x => x.ActiveIngredient)
            .NotEmpty().WithMessage("Active ingredient is required.")
            .MaximumLength(200).WithMessage("Active ingredient must not exceed 200 characters.");

        RuleFor(x => x.Strength)
            .NotEmpty().WithMessage("Strength is required.")
            .MaximumLength(50).WithMessage("Strength must not exceed 50 characters.");

        RuleFor(x => x.DosageForm)
            .NotEmpty().WithMessage("Dosage form is required.")
            .MaximumLength(50).WithMessage("Dosage form must not exceed 50 characters.");

        RuleFor(x => x.CategoryId)
            .NotEmpty().WithMessage("CategoryId is required.");

        RuleFor(x => x.ManufacturerId)
            .NotEmpty().WithMessage("ManufacturerId is required.");

        RuleFor(x => x.InitialBarcode)
            .MaximumLength(50).WithMessage("Barcode must not exceed 50 characters.")
            .When(x => !string.IsNullOrEmpty(x.InitialBarcode));
    }
}

public class UpdateMedicineRequestValidator : AbstractValidator<UpdateMedicineRequest>
{
    public UpdateMedicineRequestValidator()
    {
        RuleFor(x => x.BrandName)
            .NotEmpty().WithMessage("Brand name is required.")
            .MaximumLength(150).WithMessage("Brand name must not exceed 150 characters.");

        RuleFor(x => x.GenericName)
            .NotEmpty().WithMessage("Generic name is required.")
            .MaximumLength(150).WithMessage("Generic name must not exceed 150 characters.");

        RuleFor(x => x.ActiveIngredient)
            .NotEmpty().WithMessage("Active ingredient is required.")
            .MaximumLength(200).WithMessage("Active ingredient must not exceed 200 characters.");

        RuleFor(x => x.Strength)
            .NotEmpty().WithMessage("Strength is required.")
            .MaximumLength(50).WithMessage("Strength must not exceed 50 characters.");

        RuleFor(x => x.DosageForm)
            .NotEmpty().WithMessage("Dosage form is required.")
            .MaximumLength(50).WithMessage("Dosage form must not exceed 50 characters.");

        RuleFor(x => x.CategoryId)
            .NotEmpty().WithMessage("CategoryId is required.");

        RuleFor(x => x.ManufacturerId)
            .NotEmpty().WithMessage("ManufacturerId is required.");
    }
}

public class CreateCategoryRequestValidator : AbstractValidator<CreateCategoryRequest>
{
    public CreateCategoryRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Category name is required.")
            .MaximumLength(100).WithMessage("Category name must not exceed 100 characters.");

        RuleFor(x => x.Description)
            .MaximumLength(500).WithMessage("Description must not exceed 500 characters.")
            .When(x => !string.IsNullOrEmpty(x.Description));
    }
}

public class UpdateCategoryRequestValidator : AbstractValidator<UpdateCategoryRequest>
{
    public UpdateCategoryRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Category name is required.")
            .MaximumLength(100).WithMessage("Category name must not exceed 100 characters.");

        RuleFor(x => x.Description)
            .MaximumLength(500).WithMessage("Description must not exceed 500 characters.")
            .When(x => !string.IsNullOrEmpty(x.Description));
    }
}

public class CreateManufacturerRequestValidator : AbstractValidator<CreateManufacturerRequest>
{
    public CreateManufacturerRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Manufacturer name is required.")
            .MaximumLength(150).WithMessage("Manufacturer name must not exceed 150 characters.");

        RuleFor(x => x.Country)
            .NotEmpty().WithMessage("Country is required.")
            .MaximumLength(100).WithMessage("Country must not exceed 100 characters.");
    }
}

public class UpdateManufacturerRequestValidator : AbstractValidator<UpdateManufacturerRequest>
{
    public UpdateManufacturerRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Manufacturer name is required.")
            .MaximumLength(150).WithMessage("Manufacturer name must not exceed 150 characters.");

        RuleFor(x => x.Country)
            .NotEmpty().WithMessage("Country is required.")
            .MaximumLength(100).WithMessage("Country must not exceed 100 characters.");
    }
}

public class AddBarcodeRequestValidator : AbstractValidator<AddBarcodeRequest>
{
    public AddBarcodeRequestValidator()
    {
        RuleFor(x => x.Barcode)
            .NotEmpty().WithMessage("Barcode is required.")
            .MaximumLength(50).WithMessage("Barcode must not exceed 50 characters.");
    }
}

public class AddAliasRequestValidator : AbstractValidator<AddAliasRequest>
{
    public AddAliasRequestValidator()
    {
        RuleFor(x => x.Alias)
            .NotEmpty().WithMessage("Alias is required.")
            .MaximumLength(150).WithMessage("Alias must not exceed 150 characters.");
    }
}
