using Hangfire;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using RealEstateTax.Application.Common.Interfaces;
using RealEstateTax.Domain.Enums;
using RealEstateTax.Domain.Services;

namespace RealEstateTax.Infrastructure.BackgroundJobs;

/// <summary>
/// Calculates and persists late-payment penalties for all overdue bills.
/// Schedule via Hangfire recurring job (monthly recommended).
/// TODO: Verify penalty schedule with Egyptian Tax Authority before enabling in production.
/// </summary>
public class PenaltyCalculationJob
{
    private readonly IApplicationDbContext _db;
    private readonly ITaxCalculationService _taxCalc;
    private readonly ILogger<PenaltyCalculationJob> _logger;

    public PenaltyCalculationJob(
        IApplicationDbContext db,
        ITaxCalculationService taxCalc,
        ILogger<PenaltyCalculationJob> logger)
    {
        _db = db;
        _taxCalc = taxCalc;
        _logger = logger;
    }

    [DisableConcurrentExecution(timeoutInSeconds: 600)]
    [AutomaticRetry(Attempts = 1)]
    public async Task CalculateAndApplyPenaltiesAsync(CancellationToken ct = default)
    {
        var asOf = DateTime.UtcNow;

        var overdueBills = await _db.TaxBills
            .Where(b => !b.IsDeleted && b.Status == BillStatus.Overdue)
            .ToListAsync(ct);

        _logger.LogInformation("PenaltyCalculationJob: processing {Count} overdue bills as of {Date:yyyy-MM-dd}",
            overdueBills.Count, asOf);

        int updated = 0;
        foreach (var bill in overdueBills)
        {
            try
            {
                var penalty = await _taxCalc.CalculatePenaltyAsync(bill, asOf, ct);

                if (penalty != bill.PenaltyAmount)
                {
                    bill.PenaltyAmount = penalty;
                    updated++;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to calculate penalty for bill {BillId}", bill.Id);
            }
        }

        if (updated > 0)
        {
            await _db.SaveChangesAsync(ct);
            _logger.LogInformation("PenaltyCalculationJob: updated penalties on {Count} bills", updated);
        }
    }

    /// <summary>
    /// Recalculates risk scores for all properties with overdue bills.
    /// Intended to be chained after penalty calculation completes.
    /// </summary>
    [AutomaticRetry(Attempts = 0)]
    public async Task TriggerRiskRecalculationForOverdueAsync(CancellationToken ct = default)
    {
        var propertyIds = await _db.TaxBills
            .Where(b => !b.IsDeleted && b.Status == BillStatus.Overdue)
            .Select(b => b.PropertyId)
            .Distinct()
            .ToListAsync(ct);

        foreach (var propertyId in propertyIds)
            BackgroundJob.Enqueue<RiskRecalculationJob>(j => j.RecalculateAsync(propertyId, CancellationToken.None));

        _logger.LogInformation("PenaltyCalculationJob: enqueued risk recalculation for {Count} properties", propertyIds.Count);
    }
}
