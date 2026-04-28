using RealEstateTax.Domain.Entities;

namespace RealEstateTax.Domain.Services;

public interface IExemptionService
{
    /// <summary>
    /// Evaluates whether a property/taxpayer combination is eligible for exemption.
    /// TODO: Map eligibility criteria to specific articles of Egyptian Real Estate Tax Law 196/2008.
    /// </summary>
    Task<EligibilityResult> CheckEligibilityAsync(
        Property property,
        Taxpayer taxpayer,
        ExemptionRule rule,
        CancellationToken ct = default);

    /// <summary>
    /// Calculates the exemption amount to be applied against the gross tax.
    /// </summary>
    Task<decimal> CalculateExemptionAmountAsync(
        decimal grossTaxAmount,
        Exemption exemption,
        CancellationToken ct = default);
}

public record EligibilityResult(
    bool IsEligible,
    string[] SatisfiedCriteria,
    string[] FailedCriteria,
    string? RecommendedAction
);
