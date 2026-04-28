using FluentValidation;
using RealEstateTax.Application.DTOs.Appeals;
using RealEstateTax.Domain.Enums;

namespace RealEstateTax.Application.Validators;

public class SubmitAppealRequestValidator : AbstractValidator<SubmitAppealRequest>
{
    public SubmitAppealRequestValidator()
    {
        RuleFor(x => x.PropertyId).NotEmpty();
        RuleFor(x => x.TaxpayerId).NotEmpty();

        RuleFor(x => x.GroundsSummary)
            .NotEmpty()
            .MaximumLength(1000)
            .WithMessage("Grounds summary is required and must not exceed 1000 characters.");

        RuleFor(x => x.DetailedStatement)
            .MaximumLength(10000)
            .When(x => !string.IsNullOrEmpty(x.DetailedStatement));

        RuleFor(x => x.RequestedAssessmentValue)
            .GreaterThan(0)
            .When(x => x.RequestedAssessmentValue.HasValue);
    }
}

public class AppealDecisionRequestValidator : AbstractValidator<AppealDecisionRequest>
{
    private static readonly AppealStatus[] ValidDecisions =
        [AppealStatus.Approved, AppealStatus.PartiallyApproved, AppealStatus.Rejected];

    public AppealDecisionRequestValidator()
    {
        RuleFor(x => x.Decision)
            .Must(d => ValidDecisions.Contains(d))
            .WithMessage("Decision must be Approved, PartiallyApproved, or Rejected.");

        RuleFor(x => x.DecisionNotes)
            .NotEmpty()
            .MaximumLength(2000);

        RuleFor(x => x.RevisedAssessmentValue)
            .GreaterThan(0)
            .When(x => x.Decision == AppealStatus.PartiallyApproved)
            .WithMessage("Revised assessment value is required for partial approvals.");
    }
}
