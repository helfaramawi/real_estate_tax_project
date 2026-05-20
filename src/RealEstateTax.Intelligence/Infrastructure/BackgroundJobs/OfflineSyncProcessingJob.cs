using Hangfire;
using Microsoft.Extensions.Logging;
using RealEstateTax.Intelligence.Application.Interfaces;

namespace RealEstateTax.Intelligence.Infrastructure.BackgroundJobs;

public class OfflineSyncProcessingJob(
    IOfflineSyncService syncService,
    ILogger<OfflineSyncProcessingJob> logger)
{
    [DisableConcurrentExecution(timeoutInSeconds: 300)]
    public async Task ProcessPendingPacketsAsync(CancellationToken ct = default)
    {
        logger.LogDebug("Processing pending offline sync packets");
        await syncService.ProcessPendingPacketsAsync(ct);
    }
}
