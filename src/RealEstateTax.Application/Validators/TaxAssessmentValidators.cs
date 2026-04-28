using FluentValidation;
using RealEstateTax.Application.DTOs.TaxAssessments;

namespace RealEstateTax.Application.Validators;

public class GenerateTaxAssessmentRequestValidator : AbstractValidator<GenerateTaxAssessmentRequest>
{
    public GenerateTaxAssessmentRequestValidator()
    {
        RuleFor(x => x.PropertyId).NotEmpty();
        RuleFor(x => x.ValuationId).NotEmpty();

        RuleFor(x => x.TaxYear)
            .InclusiveBetween(2000, DateTime.UtcNow.Year + 1)
            .WithMessage("Tax year must be between 2000 and next year.");
    }
}

public class ApproveTaxAssessmentRequestValidator : AbstractValidator<ApproveTaxAssessmentRequest>
{
    public ApproveTaxAssessmentRequestValidator()
    {
        RuleFor(x => x.ApprovalNotes)
            .MaximumLength(2000)
            .When(x => !string.IsNullOrEmpty(x.ApprovalNotes));
    }
}
