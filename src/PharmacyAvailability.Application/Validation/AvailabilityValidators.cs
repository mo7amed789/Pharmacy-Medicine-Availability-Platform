using FluentValidation;
using PharmacyAvailability.Contracts.Availability;

namespace PharmacyAvailability.Application.Validation;

public class AvailabilitySearchRequestValidator : AbstractValidator<AvailabilitySearchRequest>
{
    public AvailabilitySearchRequestValidator()
    {
        RuleFor(x => x.Latitude)
            .InclusiveBetween(-90.0, 90.0).WithMessage("Latitude must be between -90 and 90 degrees.")
            .When(x => x.Latitude.HasValue);

        RuleFor(x => x.Longitude)
            .InclusiveBetween(-180.0, 180.0).WithMessage("Longitude must be between -180 and 180 degrees.")
            .When(x => x.Longitude.HasValue);

        RuleFor(x => x.Longitude)
            .NotNull().WithMessage("Longitude is required when Latitude is provided.")
            .When(x => x.Latitude.HasValue);

        RuleFor(x => x.Latitude)
            .NotNull().WithMessage("Latitude is required when Longitude is provided.")
            .When(x => x.Longitude.HasValue);

        RuleFor(x => x.RadiusKm)
            .GreaterThan(0.0).WithMessage("RadiusKm must be greater than zero.")
            .LessThanOrEqualTo(100.0).WithMessage("RadiusKm cannot exceed 100 km.")
            .When(x => x.RadiusKm.HasValue);

        RuleFor(x => x.Page)
            .GreaterThanOrEqualTo(1).WithMessage("Page must be at least 1.");

        RuleFor(x => x.PageSize)
            .InclusiveBetween(1, 100).WithMessage("PageSize must be between 1 and 100.");
    }
}
