using Microsoft.EntityFrameworkCore;
using RealEstateTax.Application.Common.Interfaces;
using RealEstateTax.Domain.Entities;
using RealEstateTax.Domain.Enums;
using RealEstateTax.Domain.Services;

namespace RealEstateTax.Infrastructure.Services;

/// <summary>
/// Computes multi-factor risk scores for property records.
/// TODO: Calibrate factor weights with Egyptian Tax Authority domain experts.
/// TODO: Integrate ML-based anomaly detection model for advanced risk scoring.
/// </summary>
public class RiskScoringService : IRiskScoringService
{
    private readonly IApplicationDbContext _db;

    public RiskScoringService(IApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<RiskScoringResult> ComputeRiskScoreAsync(Property property, CancellationToken ct = default)
    {
        var riskFactors = new List<string>();
        var recommendations = new List<string>();

        // Factor 1: Data completeness (0–100)
        var dataScore = ComputeDataCompletenessScore(property, riskFactors, recommendations);

        // Factor 2: Valuation consistency (0–100)
        var valuationScore = await ComputeValuationConsistencyScore(property, riskFactors, recommendations, ct);

        // Factor 3: Ownership chain integrity (0–100)
        var ownershipScore = ComputeOwnershipChainScore(property, riskFactors, recommendations);

        // Factor 4: Payment history (0–100)
        var paymentScore = await ComputePaymentHistoryScore(property, riskFactors, recommendations, ct);

        // Factor 5: GIS verification (0–100)
        var geoScore = ComputeGeoVerificationScore(property, riskFactors, recommendations);

        // Factor 6: Source system consistency (0–100)
        var sourceScore = ComputeSourceConsistencyScore(property, riskFactors, recommendations);

        // Weighted average — TODO: calibrate weights with domain experts
        var overallScore =
            dataScore * 0.20 +
            valuationScore * 0.25 +
            ownershipScore * 0.20 +
            paymentScore * 0.15 +
            geoScore * 0.10 +
            sourceScore * 0.10;

        // Invert: high raw score = low risk. Map to risk scale where 100 = highest risk.
        var riskScore = 100.0 - overallScore;
        var level = ClassifyRiskLevel(riskScore);

        return new RiskScoringResult(
            OverallScore: Math.Round(riskScore, 2),
            Level: level,
            DataCompletenessScore: Math.Round(100 - dataScore, 2),
            ValuationConsistencyScore: Math.Round(100 - valuationScore, 2),
            OwnershipChainScore: Math.Round(100 - ownershipScore, 2),
            PaymentHistoryScore: Math.Round(100 - paymentScore, 2),
            GeoVerificationScore: Math.Round(100 - geoScore, 2),
            SourceConsistencyScore: Math.Round(100 - sourceScore, 2),
            RiskFactors: [.. riskFactors],
            Recommendations: [.. recommendations]
        );
    }

    public RiskLevel ClassifyRiskLevel(double score) => score switch
    {
        < 25 => RiskLevel.Low,
        < 50 => RiskLevel.Medium,
        < 75 => RiskLevel.High,
        _ => RiskLevel.Critical
    };

    private double ComputeDataCompletenessScore(Property property, List<string> factors, List<string> recs)
    {
        double score = 100.0;

        if (string.IsNullOrEmpty(property.StreetAddress)) { score -= 15; factors.Add("MissingStreetAddress"); recs.Add("Assign street address via field survey"); }
        if (string.IsNullOrEmpty(property.Governorate)) { score -= 15; factors.Add("MissingGovernorate"); }
        if (property.BuiltUpArea <= 0) { score -= 20; factors.Add("InvalidBuiltUpArea"); recs.Add("Conduct measurement survey"); }
        if (!property.YearBuilt.HasValue) { score -= 10; factors.Add("MissingYearBuilt"); }
        if (property.Location == null) { score -= 20; factors.Add("NoLocationRecord"); recs.Add("Capture GPS coordinates"); }
        if (!property.Ownerships.Any(o => o.IsCurrent)) { score -= 20; factors.Add("NoCurrentOwner"); recs.Add("Link verified owner"); }

        return Math.Max(0, score);
    }

    private async Task<double> ComputeValuationConsistencyScore(Property property, List<string> factors, List<string> recs, CancellationToken ct)
    {
        double score = 100.0;

        var valuations = await _db.Valuations
            .Where(v => v.PropertyId == property.Id && !v.IsDeleted)
            .OrderByDescending(v => v.TaxYear)
            .Take(3)
            .ToListAsync(ct);

        if (!valuations.Any()) { score -= 40; factors.Add("NoValuationOnRecord"); recs.Add("Create property valuation"); return Math.Max(0, score); }
        if (!valuations.Any(v => v.Status == ValuationStatus.Approved)) { score -= 20; factors.Add("NoApprovedValuation"); recs.Add("Approve pending valuation"); }

        if (valuations.Count >= 2)
        {
            var diff = Math.Abs((double)(valuations[0].NetValue - valuations[1].NetValue));
            var pct = valuations[1].NetValue > 0 ? diff / (double)valuations[1].NetValue : 0;
            if (pct > 0.5) { score -= 30; factors.Add($"ValuationVariance>{pct:P0}"); recs.Add("Review unusual valuation change"); }
        }

        return Math.Max(0, score);
    }

    private double ComputeOwnershipChainScore(Property property, List<string> factors, List<string> recs)
    {
        double score = 100.0;
        var current = property.Ownerships.Where(o => o.IsCurrent && !o.IsDeleted).ToList();

        if (!current.Any()) { score -= 50; factors.Add("NoCurrentOwnership"); recs.Add("Establish ownership records"); return Math.Max(0, score); }
        if (!current.Any(o => o.IsVerified)) { score -= 25; factors.Add("UnverifiedOwnership"); recs.Add("Verify title deed documents"); }

        var totalPct = current.Sum(o => o.OwnershipPercentage);
        if (Math.Abs((double)(totalPct - 100m)) > 0.01m) { score -= 20; factors.Add($"OwnershipPercentageSum={totalPct}"); recs.Add("Reconcile ownership percentages to 100%"); }
        if (!current.Any(o => !string.IsNullOrEmpty(o.TitleDeedNumber))) { score -= 15; factors.Add("NoTitleDeedReference"); }

        return Math.Max(0, score);
    }

    private async Task<double> ComputePaymentHistoryScore(Property property, List<string> factors, List<string> recs, CancellationToken ct)
    {
        double score = 100.0;

        var bills = await _db.TaxBills
            .Where(b => b.PropertyId == property.Id && !b.IsDeleted)
            .OrderByDescending(b => b.IssueDate)
            .Take(5)
            .ToListAsync(ct);

        if (!bills.Any()) return score;

        var overdueBills = bills.Count(b => b.Status == BillStatus.Overdue);
        if (overdueBills > 0) { score -= overdueBills * 20; factors.Add($"{overdueBills}OverdueBills"); recs.Add("Follow up on overdue payments"); }

        var paidOnTime = bills.Count(b => b.Status == BillStatus.Paid);
        if (paidOnTime == 0 && bills.Count > 0) { score -= 20; factors.Add("NoBillsPaidOnTime"); }

        return Math.Max(0, score);
    }

    private double ComputeGeoVerificationScore(Property property, List<string> factors, List<string> recs)
    {
        double score = 100.0;
        var loc = property.Location;

        if (loc == null) { score -= 60; factors.Add("NoLocationRecord"); recs.Add("Capture GPS on next field survey"); return Math.Max(0, score); }
        if (!loc.IsVerified) { score -= 20; factors.Add("UnverifiedLocation"); recs.Add("Verify GPS coordinates via field survey"); }
        if (loc.Coordinates == null && !loc.Latitude.HasValue) { score -= 30; factors.Add("NoCoordinates"); }
        if (loc.Boundary == null) { score -= 10; factors.Add("NoBoundaryPolygon"); }

        return Math.Max(0, score);
    }

    private double ComputeSourceConsistencyScore(Property property, List<string> factors, List<string> recs)
    {
        double score = 100.0;
        var records = property.SourceRecords.Where(r => !r.IsDeleted).ToList();

        if (!records.Any()) { score -= 30; factors.Add("NoSourceRecords"); recs.Add("Import source records from utility/cadastral systems"); return Math.Max(0, score); }

        if (records.Count == 1) { score -= 15; factors.Add("SingleSourceOnly"); recs.Add("Cross-verify with additional source systems"); }

        var areaValues = records.Where(r => r.AreaFromSource.HasValue).Select(r => (double)r.AreaFromSource!.Value).ToList();
        if (areaValues.Count >= 2)
        {
            var cv = StandardDeviation(areaValues) / (areaValues.Average() + 0.001);
            if (cv > 0.3) { score -= 20; factors.Add($"AreaDiscrepancyAcrossSources(CV={cv:F2})"); recs.Add("Resolve area discrepancy across source systems"); }
        }

        return Math.Max(0, score);
    }

    private static double StandardDeviation(IEnumerable<double> values)
    {
        var list = values.ToList();
        var avg = list.Average();
        return Math.Sqrt(list.Sum(v => Math.Pow(v - avg, 2)) / list.Count);
    }
}
