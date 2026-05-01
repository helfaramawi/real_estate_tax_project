using RealEstateTax.Intelligence.Application.DTOs;
using RealEstateTax.Intelligence.Domain.Entities;
using RealEstateTax.Intelligence.Domain.Enums;

namespace RealEstateTax.Intelligence.Application.Interfaces;

public interface IIntelligenceService
{
    Task<List<PredictionResult>> GetPredictionsAsync(Guid propertyId, CancellationToken ct = default);
    Task TriggerPredictionAsync(Guid propertyId, List<PredictionType> types, CancellationToken ct = default);
    Task<PagedPredictionsDto> GetPendingReviewAsync(int page, int pageSize, PredictionType? type, double? minScore, CancellationToken ct = default);
    Task<PredictionResult?> ReviewPredictionAsync(Guid predictionId, string outcome, string? notes, Guid reviewerId, CancellationToken ct = default);
    Task<IntelligenceSummaryDto> GetDashboardSummaryAsync(CancellationToken ct = default);
    Task<List<ModelRegistration>> GetModelsAsync(CancellationToken ct = default);
    Task<ModelRegistration?> PromoteModelAsync(Guid modelId, Guid promotedBy, CancellationToken ct = default);
    Task RunBatchInferenceAsync(PredictionType type, CancellationToken ct = default);
}
