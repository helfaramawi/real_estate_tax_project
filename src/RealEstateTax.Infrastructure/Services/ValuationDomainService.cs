using Microsoft.Extensions.Logging;
using RealEstateTax.Domain.Entities;
using RealEstateTax.Domain.Enums;
using RealEstateTax.Domain.Services;

namespace RealEstateTax.Infrastructure.Services;

/// <summary>
/// Implements property valuation for Egyptian Real Estate Tax.
/// The primary method under Egyptian law is Annual Rental Value (ARV).
/// TODO: Obtain zone-specific rental value tables from the Egyptian Tax Authority.
/// </summary>
public class ValuationDomainService : IValuationService
{
    private readonly ILogger<ValuationDomainService> _logger;

    public ValuationDomainService(ILogger<ValuationDomainService> logger)
    {
        _logger = logger;
    }

    public Task<ValuationResult> ComputeValueAsync(
        Property property,
        ValuationMethod method,
        ValuationRule rule,
        CancellationToken ct = default)
    {
        return method switch
        {
            ValuationMethod.RentalValue => ComputeRentalValueAsync(property, rule),
            ValuationMethod.MarketComparison => ComputeMarketComparisonAsync(property, rule),
            ValuationMethod.CostApproach => ComputeCostApproachAsync(property, rule),
            _ => Task.FromResult(new ValuationResult(0, 0, 0, method.ToString(),
                    ["Method not fully implemented"],
                    ["Fallback to 0 — manual review required"]))
        };
    }

    private Task<ValuationResult> ComputeRentalValueAsync(Property property, ValuationRule rule)
    {
        var assumptions = new List<string>();
        var warnings = new List<string>();

        // Step 1: Determine annual rental value
        decimal annualRentalValue;
        if (rule.StandardRentPerSqM.HasValue && property.BuiltUpArea > 0)
        {
            // Use standard rent table from rule
            annualRentalValue = rule.StandardRentPerSqM.Value * property.BuiltUpArea * 12;
            assumptions.Add($"StandardRent: {rule.StandardRentPerSqM} EGP/m²/month × {property.BuiltUpArea} m² × 12");
        }
        else
        {
            // TODO: Fallback — field survey actual rent should be used here
            annualRentalValue = 0;
            warnings.Add("StandardRentPerSqM not configured — ARV = 0. Update ValuationRule with correct rates.");
        }

        // Step 2: Apply maintenance deduction
        // TODO: Verify deduction percentage per Egyptian law — typical range 30–40%
        var deductionPct = rule.DeductionPercentage > 0 ? rule.DeductionPercentage : 30.0m;
        var deductionAmount = annualRentalValue * (deductionPct / 100m);
        var netValue = annualRentalValue - deductionAmount;
        assumptions.Add($"Deduction: {deductionPct}% ({deductionAmount:F2} EGP)");

        // Apply min/max caps if configured
        if (rule.MinNetValue.HasValue && netValue < rule.MinNetValue.Value)
        {
            netValue = rule.MinNetValue.Value;
            warnings.Add($"Net value floored to minimum {rule.MinNetValue.Value} EGP");
        }
        if (rule.MaxNetValue.HasValue && netValue > rule.MaxNetValue.Value)
        {
            netValue = rule.MaxNetValue.Value;
            warnings.Add($"Net value capped to maximum {rule.MaxNetValue.Value} EGP");
        }

        return Task.FromResult(new ValuationResult(
            GrossValue: annualRentalValue,
            DeductionPercentage: deductionPct,
            NetValue: netValue,
            MethodUsed: "RentalValue",
            Assumptions: [.. assumptions],
            Warnings: [.. warnings]
        ));
    }

    private Task<ValuationResult> ComputeMarketComparisonAsync(Property property, ValuationRule rule)
    {
        // TODO: Integrate with market comparison data source
        var warnings = new[] { "MarketComparison requires comparable sales data — not yet integrated. Use RentalValue method." };
        return Task.FromResult(new ValuationResult(0, rule.DeductionPercentage, 0, "MarketComparison", [], warnings));
    }

    private Task<ValuationResult> ComputeCostApproachAsync(Property property, ValuationRule rule)
    {
        // TODO: Implement cost approach with depreciation schedule
        var warnings = new[] { "CostApproach not yet implemented. Use RentalValue method." };
        return Task.FromResult(new ValuationResult(0, rule.DeductionPercentage, 0, "CostApproach", [], warnings));
    }
}
