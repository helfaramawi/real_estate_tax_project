using FluentValidation;
using RealEstateTax.Application.DTOs.Risk;

namespace RealEstateTax.Application.Validators;

public class CreateFraudFlagRequestValidator : AbstractValidator<CreateFraudFlagRequest>
{
    public CreateFraudFlagRequestValidator()
    {
        RuleFor(x => x.PropertyId).NotEmpty();

        RuleFor(x => x.FlagType).IsInEnum();

        RuleFor(x => x.Severity).IsInEnum();

        RuleFor(x => x.Description)
            .NotEmpty()
            .MinimumLength(20)
            .MaximumLength(2000)
            .WithMessage("Description must be between 20 and 2000 characters.");

        RuleFor(x => x.Evidence)
            .MaximumLength(5000)
            .When(x => !string.IsNullOrEmpty(x.Evidence));
    }
}
