using FluentValidation;
using PharmacyAvailability.Contracts.Reservations;

namespace PharmacyAvailability.Application.Validation;

public class CreateReservationRequestValidator : AbstractValidator<CreateReservationRequest>
{
    public CreateReservationRequestValidator()
    {
        RuleFor(x => x.BranchId)
            .NotEmpty().WithMessage("BranchId is required.");

        RuleFor(x => x.MedicineId)
            .NotEmpty().WithMessage("MedicineId is required.");

        RuleFor(x => x.Quantity)
            .GreaterThan(0).WithMessage("Reservation quantity must be greater than zero.")
            .LessThanOrEqualTo(100).WithMessage("Reservation quantity cannot exceed 100 units per order.");
    }
}

public class AdminReleaseReservationRequestValidator : AbstractValidator<AdminReleaseReservationRequest>
{
    public AdminReleaseReservationRequestValidator()
    {
        RuleFor(x => x.Reason)
            .NotEmpty().WithMessage("A cancellation/release reason is required.")
            .MinimumLength(3).WithMessage("Reason must be at least 3 characters.")
            .MaximumLength(500).WithMessage("Reason must not exceed 500 characters.");
    }
}
