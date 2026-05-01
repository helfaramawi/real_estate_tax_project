using Hangfire;
using Microsoft.Extensions.Logging;
using RealEstateTax.Intelligence.Application.Interfaces;
using RealEstateTax.Intelligence.Domain.Enums;

namespace RealEstateTax.Intelligence.Infrastructure.BackgroundJobs;

public class MLInferenceJob(
    IIntelligenceService intelligenceService,
    IMLModelClient mlClient,
    ILogger<MLInferenceJob> logger)
{
    [AutomaticRetry(Attempts = 3, DelaysInSeconds = [60, 300, 900])]
    public async Task RunBatchInferenceAsync(PredictionType type, CancellationToken ct = default)
    {
        logger.LogInformation("ML batch inference job started for type={Type}", type);

        var isHealthy = await mlClient.IsHealthyAsync(ct);
        if (!isHealthy)
        {
            logger.LogWarning("ML service is not healthy — skipping inference for {Type}", type);
            return;
        }

        await intelligenceService.RunBatchInferenceAsync(type, ct);
        logger.LogInformation("ML batch inference job complete for type={Type}", type);
    }
}
