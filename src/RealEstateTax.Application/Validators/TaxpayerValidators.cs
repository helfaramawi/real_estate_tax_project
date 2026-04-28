using FluentValidation;
using RealEstateTax.Application.DTOs.Taxpayers;

namespace RealEstateTax.Application.Validators;

public class CreateTaxpayerRequestValidator : AbstractValidator<CreateTaxpayerRequest>
{
    public CreateTaxpayerRequestValidator()
    {
        RuleFor(x => x.NationalId)
            .NotEmpty()
            .Length(14)
            .Matches(@"^\d{14}$")
            .WithMessage("National ID must be exactly 14 digits.");
            // TODO: Add Egyptian National ID checksum validation algorithm

        RuleFor(x => x.FirstName)
            .NotEmpty()
            .MaximumLength(100)
            .When(x => !x.IsCorporate);

        RuleFor(x => x.LastName)
            .NotEmpty()
            .MaximumLength(100)
            .When(x => !x.IsCorporate);

        RuleFor(x => x.CompanyName)
            .NotEmpty()
            .MaximumLength(200)
            .When(x => x.IsCorporate);

        RuleFor(x => x.Email)
            .EmailAddress()
            .MaximumLength(200)
            .When(x => !string.IsNullOrEmpty(x.Email));

        RuleFor(x => x.PhoneNumber)
            .Matches(@"^(\+20|0)?1[0-2,5]\d{8}$")
            .WithMessage("Phone number must be a valid Egyptian mobile number.")
            .When(x => !string.IsNullOrEmpty(x.PhoneNumber));
    }
}

public class UpdateTaxpayerRequestValidator : AbstractValidator<UpdateTaxpayerRequest>
{
    public UpdateTaxpayerRequestValidator()
    {
        RuleFor(x => x.Email)
            .EmailAddress()
            .MaximumLength(200)
            .When(x => !string.IsNullOrEmpty(x.Email));

        RuleFor(x => x.PhoneNumber)
            .Matches(@"^(\+20|0)?1[0-2,5]\d{8}$")
            .WithMessage("Phone number must be a valid Egyptian mobile number.")
            .When(x => !string.IsNullOrEmpty(x.PhoneNumber));
    }
}
