using FluentValidation;
using RealEstateTax.Application.DTOs.Exemptions;

namespace RealEstateTax.Application.Validators;

public class SubmitExemptionRequestValidator : AbstractValidator<SubmitExemptionRequest>
{
    public SubmitExemptionRequestValidator()
    {
        RuleFor(x => x.PropertyId).NotEmpty();
        RuleFor(x => x.TaxpayerId).NotEmpty();
        RuleFor(x => x.Type).IsInEnum();

        RuleFor(x => x.JustificationSummary)
            .NotEmpty()
            .MinimumLength(20)
            .MaximumLength(2000)
            .WithMessage("Justification summary must be between 20 and 2000 characters.");

        RuleFor(x => x.EffectiveFrom)
            .LessThan(x => x.EffectiveTo)
            .WithMessage("Effective start date must be before effective end date.")
            .When(x => x.EffectiveFrom.HasValue && x.EffectiveTo.HasValue);
    }
}

public class ApproveExemptionRequestValidator : AbstractValidator<ApproveExemptionRequest>
{
    public ApproveExemptionRequestValidator()
    {
        // Must provide either a percentage OR a fixed amount, not neither
        RuleFor(x => x)
            .Must(x => x.ExemptionPercentage.HasValue || x.ExemptAmount.HasValue)
            .WithMessage("Either ExemptionPercentage or ExemptAmount must be provided.");

        // Must not provide both
        RuleFor(x => x)
            .Must(x => !(x.ExemptionPercentage.HasValue && x.ExemptAmount.HasValue))
            .WithMessage("Provide either ExemptionPercentage or ExemptAmount, not both.");

        RuleFor(x => x.ExemptionPercentage)
            .InclusiveBetween(0.01m, 100m)
            .WithMessage("Exemption percentage must be between 0.01 and 100.")
            .When(x => x.ExemptionPercentage.HasValue);

        RuleFor(x => x.ExemptAmount)
            .GreaterThan(0)
            .When(x => x.ExemptAmount.HasValue);

        RuleFor(x => x.EffectiveFrom)
            .LessThan(x => x.EffectiveTo)
            .WithMessage("Effective start date must be before effective end date.")
            .When(x => x.EffectiveFrom.HasValue && x.EffectiveTo.HasValue);

        RuleFor(x => x.ApprovalNotes)
            .MaximumLength(2000)
            .When(x => !string.IsNullOrEmpty(x.ApprovalNotes));
    }
}

public class RejectExemptionRequestValidator : AbstractValidator<RejectExemptionRequest>
{
    public RejectExemptionRequestValidator()
    {
        RuleFor(x => x.RejectionReason)
            .NotEmpty()
            .MinimumLength(10)
            .MaximumLength(1000)
            .WithMessage("Rejection reason must be between 10 and 1000 characters.");
    }
}
