using Hangfire;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using RealEstateTax.Application.Common.Interfaces;
using RealEstateTax.Domain.Enums;

namespace RealEstateTax.Infrastructure.BackgroundJobs;

/// <summary>
/// Sends payment reminder notifications for bills due within the configured window.
/// Schedule via Hangfire recurring job (daily recommended).
/// </summary>
public class BillReminderJob
{
    private const int DaysBeforeDueWarning = 30;
    private const int DaysBeforeDueUrgent = 7;

    private readonly IApplicationDbContext _db;
    private readonly INotificationService _notifications;
    private readonly ILogger<BillReminderJob> _logger;

    public BillReminderJob(
        IApplicationDbContext db,
        INotificationService notifications,
        ILogger<BillReminderJob> logger)
    {
        _db = db;
        _notifications = notifications;
        _logger = logger;
    }

    [DisableConcurrentExecution(timeoutInSeconds: 300)]
    public async Task SendRemindersAsync(CancellationToken ct = default)
    {
        var today = DateTime.UtcNow.Date;
        var warningCutoff = today.AddDays(DaysBeforeDueWarning);
        var urgentCutoff = today.AddDays(DaysBeforeDueUrgent);

        var dueBills = await _db.TaxBills
            .Include(b => b.Taxpayer)
            .Where(b => !b.IsDeleted
                && b.Status == BillStatus.Issued
                && b.DueDate >= today
                && b.DueDate <= warningCutoff)
            .ToListAsync(ct);

        _logger.LogInformation("BillReminderJob: {Count} bills due within {Days} days", dueBills.Count, DaysBeforeDueWarning);

        foreach (var bill in dueBills)
        {
            try
            {
                var isUrgent = bill.DueDate <= urgentCutoff;
                var channel = isUrgent ? NotificationChannel.SMS : NotificationChannel.Email;

                await _notifications.SendAsync(
                    taxpayerId: bill.TaxpayerId,
                    type: NotificationType.BillReminder,
                    channel: channel,
                    subject: isUrgent
                        ? $"URGENT: Tax Bill {bill.BillNumber} due in {(bill.DueDate.Date - today).Days} days"
                        : $"Reminder: Tax Bill {bill.BillNumber} due on {bill.DueDate:dd MMM yyyy}",
                    body: $"Dear taxpayer, your tax bill {bill.BillNumber} for {bill.TaxYear} " +
                          $"of {bill.RemainingAmount:N2} EGP is due on {bill.DueDate:dd MMM yyyy}. " +
                          $"Please pay at your earliest convenience to avoid penalties.",
                    entityType: "TaxBill",
                    entityId: bill.Id,
                    ct: ct);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send reminder for bill {BillId}", bill.Id);
            }
        }
    }

    /// <summary>
    /// Marks issued bills past their due date as Overdue.
    /// </summary>
    [DisableConcurrentExecution(timeoutInSeconds: 120)]
    public async Task MarkOverdueBillsAsync(CancellationToken ct = default)
    {
        var today = DateTime.UtcNow.Date;

        var overdueBills = await _db.TaxBills
            .Where(b => !b.IsDeleted
                && b.Status == BillStatus.Issued
                && b.DueDate < today)
            .ToListAsync(ct);

        if (overdueBills.Count == 0) return;

        foreach (var bill in overdueBills)
            bill.Status = BillStatus.Overdue;

        var saved = await _db.SaveChangesAsync(ct);
        _logger.LogInformation("BillReminderJob: marked {Count} bills as Overdue", saved);
    }
}
