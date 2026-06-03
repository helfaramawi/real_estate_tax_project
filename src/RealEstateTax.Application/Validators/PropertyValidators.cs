using FluentValidation;
using RealEstateTax.Application.DTOs.Properties;

namespace RealEstateTax.Application.Validators;

public class CreatePropertyRequestValidator : AbstractValidator<CreatePropertyRequest>
{
    public CreatePropertyRequestValidator()
    {
        RuleFor(x => x.Type)
            .IsInEnum();

        RuleFor(x => x.BuiltUpArea)
            .GreaterThan(0)
            .LessThanOrEqualTo(1_000_000)
            .WithMessage("Built-up area must be between 0 and 1,000,000 m².");

        RuleFor(x => x.LandArea)
            .GreaterThan(0)
            .When(x => x.LandArea.HasValue);

        RuleFor(x => x.YearBuilt)
            .InclusiveBetween(1800, DateTime.UtcNow.Year)
            .When(x => x.YearBuilt.HasValue);

        RuleFor(x => x.StreetAddress)
            .NotEmpty()
            .WithMessage("Street address is required for property registration.");

        RuleFor(x => x.City)
            .NotEmpty()
            .WithMessage("City is required for property registration.");

        RuleFor(x => x.Governorate)
            .NotEmpty()
            .WithMessage("Governorate is required for property registration.");

        RuleFor(x => x.Latitude)
            .NotNull()
            .WithMessage("GPS latitude is required. Select the property location on the map.");

        RuleFor(x => x.Longitude)
            .NotNull()
            .WithMessage("GPS longitude is required. Select the property location on the map.");

        RuleFor(x => x.Latitude)
            .InclusiveBetween(-90, 90)
            .When(x => x.Latitude.HasValue);

        RuleFor(x => x.Longitude)
            .InclusiveBetween(-180, 180)
            .When(x => x.Longitude.HasValue);

        // Egypt bounding box validation
        RuleFor(x => x.Latitude)
            .InclusiveBetween(22.0, 31.9)
            .WithMessage("Latitude must be within Egypt's geographic bounds (22°N – 31.9°N).")
            .When(x => x.Latitude.HasValue);

        RuleFor(x => x.Longitude)
            .InclusiveBetween(24.7, 37.1)
            .WithMessage("Longitude must be within Egypt's geographic bounds (24.7°E – 37.1°E).")
            .When(x => x.Longitude.HasValue);
    }
}

public class LinkOwnerRequestValidator : AbstractValidator<LinkOwnerRequest>
{
    public LinkOwnerRequestValidator()
    {
        RuleFor(x => x.TaxpayerId).NotEmpty();

        RuleFor(x => x.OwnershipType).IsInEnum();

        RuleFor(x => x.OwnershipPercentage)
            .InclusiveBetween(0.01m, 100m)
            .WithMessage("Ownership percentage must be between 0.01 and 100.");

        RuleFor(x => x.OwnershipStartDate)
            .LessThanOrEqualTo(DateTime.UtcNow)
            .WithMessage("Ownership start date cannot be in the future.");
    }
}
