using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using RealEstateTax.Intelligence.Application.DTOs;
using RealEstateTax.Intelligence.Application.Interfaces;

namespace RealEstateTax.Intelligence.Infrastructure.MLClient;

public class MLModelHttpClient(HttpClient http, ILogger<MLModelHttpClient> logger) : IMLModelClient
{
    // Python ML service uses snake_case field names; .NET 8 SnakeCaseLower converts
    // PascalCase → snake_case on both serialization and deserialization.
    private static readonly JsonSerializerOptions _snakeCase = new(JsonSerializerDefaults.Web)
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
    };

    public async Task<List<PredictionResponseDto>> PredictBatchAsync(
        PredictionRequestDto request, CancellationToken ct = default)
    {
        try
        {
            var response = await http.PostAsJsonAsync("/predict/batch", request, _snakeCase, ct);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<List<PredictionResponseDto>>(_snakeCase, ct) ?? [];
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
