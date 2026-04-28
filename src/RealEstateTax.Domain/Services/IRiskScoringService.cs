using RealEstateTax.Domain.Entities;
using RealEstateTax.Domain.Enums;

namespace RealEstateTax.Domain.Services;

public interface IRiskScoringService
{
    /// <summary>
    /// Computes an overall risk score for a property.
    /// Score 0–100; higher = riskier.
    /// TODO: Calibrate weight factors with domain experts from Egyptian Tax Authority.
    /// </summary>
    Task<RiskScoringResult> ComputeRiskScoreAsync(Property property, CancellationToken ct = default);

    /// <summary>
    /// Determines appropriate risk level based on score thresholds.
    /// </summary>
    RiskLevel ClassifyRiskLevel(double score);
}

public record RiskScoringResult(
    double OverallScore,
    RiskLevel Level,
    double DataCompletenessScore,
    double ValuationConsistencyScore,
    double OwnershipChainScore,
    double PaymentHistoryScore,
    double GeoVerificationScore,
    double SourceConsistencyScore,
    string[] RiskFactors,
    string[] Recommendations
);
