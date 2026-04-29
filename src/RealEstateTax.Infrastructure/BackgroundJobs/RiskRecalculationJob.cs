using Hangfire;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using RealEstateTax.Application.Common.Interfaces;
using RealEstateTax.Domain.Entities;
using RealEstateTax.Domain.Services;

namespace RealEstateTax.Infrastructure.BackgroundJobs;

/// <summary>
/// Recomputes and persists the risk score for a single property.
/// Enqueued by RiskAppService.RecalculateAsync and PenaltyCalculationJob.
/// </summary>
public class RiskRecalculationJob
{
    private readonly IApplicationDbContext _db;
    private readonly IRiskScoringService _scorer;
    private readonly ILogger<RiskRecalculationJob> _logger;

    public RiskRecalculationJob(
        IApplicationDbContext db,
        IRiskScoringService scorer,
        ILogger<RiskRecalculationJob> logger)
    {
        _db = db;
        _scorer = scorer;
        _logger = logger;
    }

    [AutomaticRetry(Attempts = 2, DelaysInSeconds = [120, 600])]
    public async Task RecalculateAsync(Guid propertyId, CancellationToken ct = default)
    {
        var property = await _db.Properties
            .Include(p => p.Ownerships)
            .Include(p => p.Location)
            .Include(p => p.SourceRecords)
            .FirstOrDefaultAsync(p => p.Id == propertyId && !p.IsDeleted, ct);

        if (property is null)
        {
            _logger.LogWarning("RiskRecalculationJob: property {Id} not found", propertyId);
            return;
        }

        var result = await _scorer.ComputeRiskScoreAsync(property, ct);

        var existing = await _db.RiskScores
            .FirstOrDefaultAsync(r => r.PropertyId == propertyId && !r.IsDeleted, ct);

        if (existing is not null)
        {
            existing.Score = result.OverallScore;
            existing.Level = result.Level;
            existing.DataCompletenessScore = result.DataCompletenessScore;
            existing.ValuationConsistencyScore = result.ValuationConsistencyScore;
            existing.OwnershipChainScore = result.OwnershipChainScore;
            existing.PaymentHistoryScore = result.PaymentHistoryScore;
            existing.GeoVerificationScore = result.GeoVerificationScore;
            existing.SourceConsistencyScore = result.SourceConsistencyScore;
            existing.RiskFactors = System.Text.Json.JsonSerializer.Serialize(result.RiskFactors);
            existing.Recommendations = System.Text.Json.JsonSerializer.Serialize(result.Recommendations);
            existing.CalculatedAt = DateTime.UtcNow;
        }
        else
        {
            await _db.RiskScores.AddAsync(new RiskScore
            {
                Id = Guid.NewGuid(),
                PropertyId = propertyId,
                Score = result.OverallScore,
                Level = result.Level,
                DataCompletenessScore = result.DataCompletenessScore,
                ValuationConsistencyScore = result.ValuationConsistencyScore,
                OwnershipChainScore = result.OwnershipChainScore,
                PaymentHistoryScore = result.PaymentHistoryScore,
                GeoVerificationScore = result.GeoVerificationScore,
                SourceConsistencyScore = result.SourceConsistencyScore,
                RiskFactors = System.Text.Json.JsonSerializer.Serialize(result.RiskFactors),
                Recommendations = System.Text.Json.JsonSerializer.Serialize(result.Recommendations),
                CalculatedAt = DateTime.UtcNow
            }, ct);
        }

        await _db.SaveChangesAsync(ct);
        _logger.LogInformation("RiskRecalculationJob: property {Id} risk score = {Score:F1} ({Level})",
            propertyId, result.OverallScore, result.Level);
    }
}
