using RealEstateTax.Domain.Entities;
using RealEstateTax.Domain.Services;

namespace RealEstateTax.Infrastructure.Services;

/// <summary>
/// Implements Egyptian Real Estate Tax calculation.
/// TODO: All rates, thresholds, and deductions must be verified against:
///   - Law 196/2008 (Real Estate Tax Law)
///   - Ministry of Finance implementing regulations
///   - Any subsequent amendments or annual ministerial decrees
/// </summary>
public class TaxCalculationService : ITaxCalculationService
{
    public Task<TaxCalculationResult> CalculateAsync(
        Property property,
        Valuation valuation,
        TaxRule taxRule,
        Exemption? activeExemption,
        CancellationToken ct = default)
    {
        var appliedRules = new List<string>();
        var warnings = new List<string>();

        // Step 1: Use the net taxable value from valuation
        var taxableValue = valuation.NetValue;
        appliedRules.Add($"TaxRule: {taxRule.Code}");

        // Step 2: Check minimum taxable value threshold
        // TODO: Verify threshold amount — placeholder 24,000 EGP annual rental value
        if (taxRule.MinTaxableValue.HasValue && taxableValue < taxRule.MinTaxableValue)
        {
            warnings.Add($"Taxable value {taxableValue:F2} EGP is below minimum threshold {taxRule.MinTaxableValue:F2} EGP. No tax applicable.");
            return Task.FromResult(new TaxCalculationResult(taxableValue, 0, 0, 0, 0, [.. appliedRules], [.. warnings]));
        }

        // Step 3: Apply tax rate
        // TODO: Verify rate — Egyptian law may use progressive bracket rates, not flat rate
        var taxRate = taxRule.TaxRate / 100m;
        var grossTaxAmount = taxableValue * taxRate;
        appliedRules.Add($"Rate: {taxRule.TaxRate}%");

        // Step 4: Apply exemption if any
        decimal exemptionAmount = 0m;
        if (activeExemption is { Status: Domain.Enums.ExemptionStatus.Approved })
        {
            if (activeExemption.ExemptionPercentage.HasValue)
            {
                exemptionAmount = grossTaxAmount * (activeExemption.ExemptionPercentage.Value / 100m);
                appliedRules.Add($"Exemption: {activeExemption.ExemptionPercentage}% ({activeExemption.Type})");
            }
            else if (activeExemption.ExemptAmount.HasValue)
            {
                exemptionAmount = Math.Min(activeExemption.ExemptAmount.Value, grossTaxAmount);
                appliedRules.Add($"ExemptionFixed: {activeExemption.ExemptAmount} EGP");
            }
        }

        var netTaxAmount = Math.Max(0, grossTaxAmount - exemptionAmount);

        return Task.FromResult(new TaxCalculationResult(
            TaxableValue: taxableValue,
            TaxRate: taxRule.TaxRate,
            GrossTaxAmount: grossTaxAmount,
            ExemptionAmount: exemptionAmount,
            NetTaxAmount: netTaxAmount,
            AppliedRules: [.. appliedRules],
            Warnings: [.. warnings]
        ));
    }

    public Task<decimal> CalculatePenaltyAsync(TaxBill bill, DateTime asOfDate, CancellationToken ct = default)
    {
        if (bill.Status != Domain.Enums.BillStatus.Overdue || asOfDate <= bill.DueDate)
            return Task.FromResult(0m);

        // TODO: Verify late payment penalty rate and schedule per Egyptian tax regulations
        // Placeholder: 1% per month, capped at 50%
        var monthsOverdue = (int)((asOfDate - bill.DueDate).TotalDays / 30.0);
        const decimal monthlyPenaltyRate = 0.01m;  // TODO: VERIFY
        const decimal maxPenaltyRate = 0.50m;       // TODO: VERIFY

        var penaltyRate = Math.Min(monthlyPenaltyRate * monthsOverdue, maxPenaltyRate);
        var penalty = bill.RemainingAmount * penaltyRate;

        return Task.FromResult(Math.Round(penalty, 2));
    }
}
