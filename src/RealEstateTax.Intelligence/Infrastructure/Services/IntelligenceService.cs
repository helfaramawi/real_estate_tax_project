using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using RealEstateTax.Intelligence.Application.DTOs;
using RealEstateTax.Intelligence.Application.Interfaces;
using RealEstateTax.Intelligence.Domain.Entities;
using RealEstateTax.Intelligence.Domain.Enums;
using RealEstateTax.Intelligence.Infrastructure.Persistence;

namespace RealEstateTax.Intelligence.Infrastructure.Services;

public class IntelligenceService(
    IntelligenceDbContext intelDb,
    IFeatureStoreService featureStore,
    IMLModelClient mlClient,
    ILogger<IntelligenceService> logger) : IIntelligenceService
{
    private const string CurrentFeatureVersion = "v1";

    public Task<List<PredictionResult>> GetPredictionsAsync(Guid propertyId, CancellationToken ct = default)
        => intelDb.PredictionResults
            .Where(p => p.PropertyId == propertyId)
            .Include(p => p.Model)
            .OrderByDescending(p => p.PredictedAt)
            .AsNoTracking()
            .ToListAsync(ct);

    public async Task TriggerPredictionAsync(
        Guid propertyId, List<PredictionType> types, CancellationToken ct = default)
    {
        foreach (var type in types)
        {
            var model = await intelDb.ModelRegistry
                .FirstOrDefaultAsync(m => m.PredictionType == type
                    && m.Status == ModelStatus.Production, ct);

            if (model is null)
            {
                logger.LogWarning("No production model for {Type} — skipping on-demand prediction", type);
                continue;
            }

            var features = await featureStore.ComputeFeaturesAsync([propertyId], CurrentFeatureVersion, ct);
            if (features.Count == 0) continue;

            var request = BuildPredictionRequest(model, features);
            var responses = await mlClient.PredictBatchAsync(request, ct);

            var predictions = responses.Select(r => new PredictionResult
            {
                PropertyId = propertyId,
                ModelId = model.Id,
                PredictionType = type,
                Score = r.Score,
                Label = r.Label,
                Confidence = r.Confidence,
                Explanation = JsonSerializer.Serialize(r.Explanation),
                FeatureSnapshot = JsonSerializer.Serialize(features.First()),
            }).ToList();

            intelDb.PredictionResults.AddRange(predictions);
        }
        await intelDb.SaveChangesAsync(ct);
    }

    public async Task<PagedPredictionsDto> GetPendingReviewAsync(
        int page, int pageSize, PredictionType? type, double? minScore, CancellationToken ct = default)
    {
        var q = intelDb.PredictionResults.Where(p => !p.IsReviewed);
        if (type.HasValue) q = q.Where(p => p.PredictionType == type);
        if (minScore.HasValue) q = q.Where(p => p.Score >= minScore);

        var total = await q.CountAsync(ct);
        var items = await q
            .OrderByDescending(p => p.Score)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .AsNoTracking()
            .ToListAsync(ct);

        return new PagedPredictionsDto
        {
            TotalCount = total,
            Page = page,
            PageSize = pageSize,
            Items = items.Select(p => new PredictionItemDto
            {
                Id = p.Id,
                PropertyId = p.PropertyId,
                PredictionType = p.PredictionType.ToString(),
                Score = p.Score,
                Label = p.Label,
                Confidence = p.Confidence,
                Explanation = p.Explanation is not null
                    ? JsonSerializer.Deserialize<Dictionary<string, double>>(p.Explanation)
                    : null,
                PredictedAt = p.PredictedAt,
                IsReviewed = p.IsReviewed,
            }).ToList(),
        };
    }

    public async Task<PredictionResult?> ReviewPredictionAsync(
        Guid predictionId, string outcome, string? notes, Guid reviewerId, CancellationToken ct = default)
    {
        var pred = await intelDb.PredictionResults.FindAsync([predictionId], ct);
        if (pred is null) return null;
        pred.IsReviewed = true;
        pred.ReviewedBy = reviewerId;
        pred.ReviewedAt = DateTime.UtcNow;
        pred.ReviewOutcome = outcome;
        pred.ReviewNotes = notes;
        await intelDb.SaveChangesAsync(ct);
        return pred;
    }

    public async Task<IntelligenceSummaryDto> GetDashboardSummaryAsync(CancellationToken ct = default)
    {
        var totalPredictions = await intelDb.PredictionResults.CountAsync(ct);
        var pendingReview = await intelDb.PredictionResults.CountAsync(p => !p.IsReviewed, ct);
        var highRisk = await intelDb.PredictionResults.CountAsync(
            p => p.PredictionType == PredictionType.RiskScore && p.Score >= 0.75, ct);
        var fraudSuspect = await intelDb.PredictionResults.CountAsync(
            p => p.PredictionType == PredictionType.FraudProbability && p.Score >= 0.6, ct);
        var openAnomalies = await intelDb.SpatialAnomalies.CountAsync(a => a.Status == "Open", ct);
        var criticalAnomalies = await intelDb.SpatialAnomalies
            .CountAsync(a => a.Status == "Open" && a.Severity == "Critical", ct);

        var models = await intelDb.ModelRegistry
            .Where(m => m.Status == ModelStatus.Production)
            .AsNoTracking()
            .ToListAsync(ct);

        return new IntelligenceSummaryDto
        {
            TotalPredictions = totalPredictions,
            PendingReview = pendingReview,
            HighRiskCount = highRisk,
            FraudSuspectCount = fraudSuspect,
            OpenAnomalies = openAnomalies,
            CriticalAnomalies = criticalAnomalies,
            ActiveModels = models.Select(m => new ModelSummaryDto
            {
                Id = m.Id,
                ModelName = m.ModelName,
                Version = m.Version,
                PredictionType = m.PredictionType.ToString(),
                Status = m.Status.ToString(),
                TrainedAt = m.TrainedAt,
                Metrics = m.Metrics is not null
                    ? JsonSerializer.Deserialize<Dictionary<string, double>>(m.Metrics)
                    : null,
            }).ToList(),
        };
    }

    public Task<List<ModelRegistration>> GetModelsAsync(CancellationToken ct = default)
        => intelDb.ModelRegistry.OrderByDescending(m => m.CreatedAt).AsNoTracking().ToListAsync(ct);

    public async Task<ModelRegistration?> PromoteModelAsync(Guid modelId, Guid promotedBy, CancellationToken ct = default)
    {
        var model = await intelDb.ModelRegistry.FindAsync([modelId], ct);
        if (model is null || model.Status != ModelStatus.Staged) return null;

        // Retire current production model of same type
        var current = await intelDb.ModelRegistry
            .FirstOrDefaultAsync(m => m.PredictionType == model.PredictionType
                && m.Status == ModelStatus.Production, ct);
        if (current is not null)
        {
            current.Status = ModelStatus.Retired;
            current.RetiredAt = DateTime.UtcNow;
        }

        model.Status = ModelStatus.Production;
        model.PromotedAt = DateTime.UtcNow;
        await intelDb.SaveChangesAsync(ct);
        return model;
    }

    public async Task RunBatchInferenceAsync(PredictionType type, CancellationToken ct = default)
    {
        var model = await intelDb.ModelRegistry
            .FirstOrDefaultAsync(m => m.PredictionType == type && m.Status == ModelStatus.Production, ct);

        if (model is null)
        {
            logger.LogWarning("No production model for {Type} — skipping batch inference", type);
            return;
        }

        var totalFeatures = await featureStore.CountByVersionAsync(CurrentFeatureVersion, ct);
        const int batchSize = 200;
        var batches = (int)Math.Ceiling(totalFeatures / (double)batchSize);

        logger.LogInformation("Running batch inference: type={Type}, model={Model}, batches={Batches}",
            type, model.ModelName, batches);

        for (int i = 0; i < batches; i++)
        {
            var features = await featureStore.GetByVersionAsync(
                CurrentFeatureVersion, i * batchSize, batchSize, ct);

            if (features.Count == 0) break;

            var request = BuildPredictionRequest(model, features);

            List<PredictionResponseDto> responses;
            try
            {
                responses = await mlClient.PredictBatchAsync(request, ct);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Batch inference failed at batch {Batch}/{Total}", i + 1, batches);
                continue;
            }

            var predictions = responses.Select(r => new PredictionResult
            {
                PropertyId = Guid.Parse(r.PropertyId),
                ModelId = model.Id,
                PredictionType = type,
                Score = r.Score,
                Label = r.Label,
                Confidence = r.Confidence,
                Explanation = JsonSerializer.Serialize(r.Explanation),
            }).ToList();

            intelDb.PredictionResults.AddRange(predictions);
            await intelDb.SaveChangesAsync(ct);
        }

        logger.LogInformation("Batch inference complete for {Type}", type);
    }

    private static PredictionRequestDto BuildPredictionRequest(
        ModelRegistration model, List<FeatureVector> features)
    {
        var rows = features.Select(f => new FeatureRowDto(
            f.PropertyId.ToString(),
            f.Lat, f.Lon,
            f.BuiltUpArea, f.LandArea, f.PropertyTypeCode, f.YearBuilt,
            f.DeclaredAnnualValue, f.MarketValuePerSqm,
            f.PaidOnTimeRate, f.OverdueCount,
            f.NearestNeighborDistanceM, f.NeighborsWithin100m,
            f.ExistingRiskScore, f.FraudFlagsCount,
            f.CorporateOwnerFlag, f.OwnershipChainLength, f.DaysSinceLastSurvey,
            f.SurveysCount, f.ValueVsClusterMedianPct
        )).ToList();

        return new PredictionRequestDto(model.ModelName, model.Version, rows);
    }
}
