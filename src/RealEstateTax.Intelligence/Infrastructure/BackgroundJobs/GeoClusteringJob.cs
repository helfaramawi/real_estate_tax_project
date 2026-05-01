using Hangfire;
using Microsoft.Extensions.Logging;
using RealEstateTax.Intelligence.Application.Interfaces;

namespace RealEstateTax.Intelligence.Infrastructure.BackgroundJobs;

public class GeoClusteringJob(
    IGeoIntelligenceService geoService,
    ILogger<GeoClusteringJob> logger)
{
    [DisableConcurrentExecution(timeoutInSeconds: 1800)]
    [AutomaticRetry(Attempts = 2, DelaysInSeconds = [300, 900])]
    public async Task RunClusteringAsync(CancellationToken ct = default)
    {
        logger.LogInformation("Geospatial clustering job started");
        await geoService.RunClusteringAsync(ct);
        logger.LogInformation("Geospatial clustering job complete");
    }

    public async Task UpdateGeoFenceMembershipAsync(CancellationToken ct = default)
    {
        logger.LogInformation("Geo-fence membership update started");
        await geoService.UpdatePropertyGeoFenceMembershipAsync(ct);
        logger.LogInformation("Geo-fence membership update complete");
    }
}
