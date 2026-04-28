using FluentValidation;
using RealEstateTax.Application.DTOs.Valuations;

namespace RealEstateTax.Application.Validators;

public class CreateValuationRequestValidator : AbstractValidator<CreateValuationRequest>
{
    public CreateValuationRequestValidator()
    {
        RuleFor(x => x.PropertyId).NotEmpty();

        RuleFor(x => x.Method).IsInEnum();

        RuleFor(x => x.TaxYear)
            .InclusiveBetween(2000, DateTime.UtcNow.Year + 1)
            .WithMessage("Tax year must be between 2000 and next year.");

        RuleFor(x => x.ValuationDate)
            .LessThanOrEqualTo(DateTime.UtcNow.AddDays(1))
            .WithMessage("Valuation date cannot be in the future.");

        RuleFor(x => x.TotalArea)
            .GreaterThan(0)
            .LessThanOrEqualTo(1_000_000)
            .WithMessage("Total area must be between 0 and 1,000,000 m².");

        RuleFor(x => x.RentableArea)
            .GreaterThan(0)
            .LessThanOrEqualTo(x => x.TotalArea)
            .WithMessage("Rentable area must not exceed total area.")
            .When(x => x.RentableArea.HasValue);

        RuleFor(x => x.AnnualRentalValue)
            .GreaterThanOrEqualTo(0)
            .When(x => x.AnnualRentalValue.HasValue);

        RuleFor(x => x.MarketValuePerSqM)
            .GreaterThan(0)
            .When(x => x.MarketValuePerSqM.HasValue);

        RuleFor(x => x.CapitalizationRate)
            .InclusiveBetween(0.01m, 1.0m)
            .WithMessage("Capitalization rate must be between 0.01 and 1.0 (1%–100%).")
            .When(x => x.CapitalizationRate.HasValue);
    }
}

public class RejectValuationRequestValidator : AbstractValidator<RejectValuationRequest>
{
    public RejectValuationRequestValidator()
    {
        RuleFor(x => x.RejectionReason)
            .NotEmpty()
            .MinimumLength(10)
            .MaximumLength(1000)
            .WithMessage("Rejection reason must be between 10 and 1000 characters.");
    }
}
