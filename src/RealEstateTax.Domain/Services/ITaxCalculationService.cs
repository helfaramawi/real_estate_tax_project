using RealEstateTax.Domain.Entities;

namespace RealEstateTax.Domain.Services;

public interface ITaxCalculationService
{
    /// <summary>
    /// Calculates the annual tax amount for a property.
    /// TODO: Implement with verified Egyptian Real Estate Tax Law 196/2008 formula:
    ///   Tax = (Annual Rental Value × (1 - DeductionRate)) × TaxRate
    /// Deduction rate and tax rate must come from TaxRule and ValuationRule tables.
    /// </summary>
    Task<TaxCalculationResult> CalculateAsync(
        Property property,
        Valuation valuation,
        TaxRule taxRule,
        Exemption? activeExemption,
        CancellationToken ct = default);

    /// <summary>
    /// Calculates late payment penalty.
    /// TODO: Verify penalty rate per Egyptian tax regulations.
    /// </summary>
    Task<decimal> CalculatePenaltyAsync(TaxBill bill, DateTime asOfDate, CancellationToken ct = default);
}

public record TaxCalculationResult(
    decimal TaxableValue,
    decimal TaxRate,
    decimal GrossTaxAmount,
    decimal ExemptionAmount,
    decimal NetTaxAmount,
    string[] AppliedRules,
    string[] Warnings
);
