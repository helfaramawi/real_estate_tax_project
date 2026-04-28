using FluentValidation;
using RealEstateTax.Application.DTOs.Payments;

namespace RealEstateTax.Application.Validators;

public class RegisterPaymentRequestValidator : AbstractValidator<RegisterPaymentRequest>
{
    public RegisterPaymentRequestValidator()
    {
        RuleFor(x => x.TaxBillId).NotEmpty();

        RuleFor(x => x.Method).IsInEnum();

        RuleFor(x => x.Amount)
            .GreaterThan(0)
            .WithMessage("Payment amount must be greater than zero.");

        RuleFor(x => x.PaidAt)
            .LessThanOrEqualTo(DateTime.UtcNow)
            .WithMessage("Payment date cannot be in the future.")
            .When(x => x.PaidAt.HasValue);
    }
}
