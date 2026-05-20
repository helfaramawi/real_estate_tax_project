using Hangfire;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using RealEstateTax.Intelligence.Application.Interfaces;
using RealEstateTax.Infrastructure.Persistence;

namespace RealEstateTax.Intelligence.Infrastructure.BackgroundJobs;

public class FeatureComputationJob(
    ApplicationDbContext appDb,
    IFeatureStoreService featureStore,
    ILogger<FeatureComputationJob> logger)
{
    private const string FeatureVersion = "v1";
    private const int BatchSize = 500;

    [DisableConcurrentExecution(timeoutInSeconds: 3600)]
    [AutomaticRetry(Attempts = 1, DelaysInSeconds = [600])]
    public async Task ComputeAllFeaturesAsync(CancellationToken ct = default)
    {
        logger.LogInformation("Feature computation job started, version={Version}", FeatureVersion);

        var propertyIds = await appDb.Properties
            .Where(p => !p.IsDeleted && p.Location != null)
            .Select(p => p.Id)
            .ToListAsync(ct);

        var total = propertyIds.Count;
        var batches = (int)Math.Ceiling(total / (double)BatchSize);
        logger.LogInformation("Processing {Total} properties in {Batches} batches", total, batches);

        for (int i = 0; i < batches; i++)
        {
            var batch = propertyIds.Skip(i * BatchSize).Take(BatchSize).ToList();
            try
            {
                var features = await featureStore.ComputeFeaturesAsync(batch, FeatureVersion, ct);
                await featureStore.BulkUpsertAsync(features, ct);
                logger.LogDebug("Batch {Batch}/{Total} complete", i + 1, batches);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Feature computation failed for batch {Batch}/{Total}", i + 1, batches);
            }
        }

        logger.LogInformation("Feature computation job complete for {Total} properties", total);
    }
}
