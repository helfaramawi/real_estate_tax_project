using RealEstateTax.Intelligence.Application.DTOs;

namespace RealEstateTax.Intelligence.Application.Interfaces;

public interface IMLModelClient
{
    Task<List<PredictionResponseDto>> PredictBatchAsync(PredictionRequestDto request, CancellationToken ct = default);
    Task<bool> IsHealthyAsync(CancellationToken ct = default);
}
