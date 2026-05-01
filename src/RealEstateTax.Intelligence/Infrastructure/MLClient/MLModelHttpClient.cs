using System.Net.Http.Json;
using Microsoft.Extensions.Logging;
using RealEstateTax.Intelligence.Application.DTOs;
using RealEstateTax.Intelligence.Application.Interfaces;

namespace RealEstateTax.Intelligence.Infrastructure.MLClient;

public class MLModelHttpClient(HttpClient http, ILogger<MLModelHttpClient> logger) : IMLModelClient
{
    public async Task<List<PredictionResponseDto>> PredictBatchAsync(
        PredictionRequestDto request, CancellationToken ct = default)
    {
        try
        {
            var response = await http.PostAsJsonAsync("/predict/batch", request, ct);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<List<PredictionResponseDto>>(ct) ?? [];
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "ML service batch prediction failed for model {Model} {Version}",
                request.ModelName, request.ModelVersion);
            throw;
        }
    }

    public async Task<bool> IsHealthyAsync(CancellationToken ct = default)
    {
        try
        {
            var response = await http.GetAsync("/health", ct);
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }
}
