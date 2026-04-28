using Hangfire;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using RealEstateTax.Application.Common.Interfaces;
using RealEstateTax.Domain.Enums;

namespace RealEstateTax.Infrastructure.BackgroundJobs;

/// <summary>
/// Processes queued inbound/outbound integration requests with automatic retry.
/// Enqueued by IntegrationAppService.ReceiveAsync and RetryAsync.
/// TODO: Implement actual HTTP call to external entity BaseUrl using HttpClientFactory.
/// </summary>
public class IntegrationProcessingJob
{
    private readonly IApplicationDbContext _db;
    private readonly ILogger<IntegrationProcessingJob> _logger;

    public IntegrationProcessingJob(IApplicationDbContext db, ILogger<IntegrationProcessingJob> logger)
    {
        _db = db;
        _logger = logger;
    }

    [AutomaticRetry(Attempts = 3, DelaysInSeconds = [60, 300, 900])]
    [DisableConcurrentExecution(timeoutInSeconds: 30)]
    public async Task ProcessAsync(Guid integrationRequestId, CancellationToken ct = default)
    {
        var request = await _db.IntegrationRequests
            .Include(r => r.ExternalEntity)
            .FirstOrDefaultAsync(r => r.Id == integrationRequestId, ct);

        if (request is null)
        {
            _logger.LogWarning("IntegrationRequest {Id} not found — skipping", integrationRequestId);
            return;
        }

        if (request.Status is IntegrationRequestStatus.Processed)
        {
            _logger.LogInformation("IntegrationRequest {Id} already processed", integrationRequestId);
            return;
        }

        _logger.LogInformation(
            "Processing integration request {Id} [{Direction}] for entity {Entity}",
            request.Id, request.Direction, request.ExternalEntity?.Code);

        try
        {
            request.Status = IntegrationRequestStatus.Processing;
            await _db.SaveChangesAsync(ct);

            // TODO: Route to the correct handler based on Direction and OperationType
            // e.g. if (request.Direction == IntegrationDirection.Inbound) await HandleInboundAsync(request, ct);
            //      else await HandleOutboundAsync(request, ct);

            request.Status = IntegrationRequestStatus.Processed;
            request.ProcessedAt = DateTime.UtcNow;
            request.RetryCount++;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process IntegrationRequest {Id} (attempt {Retry})",
                integrationRequestId, request.RetryCount + 1);

            request.Status = IntegrationRequestStatus.Failed;
            request.ErrorMessage = ex.Message;
            request.RetryCount++;
            request.NextRetryAt = DateTime.UtcNow.AddSeconds(GetBackoffSeconds(request.RetryCount));

            throw; // Let Hangfire handle retry
        }
        finally
        {
            await _db.SaveChangesAsync(ct);
        }
    }

    private static int GetBackoffSeconds(int retryCount) => retryCount switch
    {
        1 => 60,
        2 => 300,
        _ => 900
    };
}
