using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using RealEstateTax.Application.Common.Interfaces;
using RealEstateTax.Domain.Entities;
using RealEstateTax.Domain.Enums;

namespace RealEstateTax.Infrastructure.Services;

public class NotificationService : INotificationService
{
    private readonly IApplicationDbContext _db;
    private readonly ILogger<NotificationService> _logger;

    public NotificationService(IApplicationDbContext db, ILogger<NotificationService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task SendAsync(
        Guid taxpayerId,
        NotificationType type,
        NotificationChannel channel,
        string subject,
        string body,
        string? entityType = null,
        Guid? entityId = null,
        CancellationToken ct = default)
    {
        var taxpayer = await _db.Taxpayers.FindAsync([taxpayerId], ct);
        if (taxpayer == null)
        {
            _logger.LogWarning("Notification skipped — taxpayer {TaxpayerId} not found", taxpayerId);
            return;
        }

        var recipientAddress = channel == NotificationChannel.Email
            ? taxpayer.Email
            : taxpayer.PhoneNumber;

        if (string.IsNullOrEmpty(recipientAddress))
        {
            _logger.LogWarning("Notification skipped — no recipient address for taxpayer {TaxpayerId} via {Channel}", taxpayerId, channel);
            return;
        }

        var notification = new Notification
        {
            TaxpayerId = taxpayerId,
            Type = type,
            Channel = channel,
            Status = NotificationStatus.Pending,
            Subject = subject,
            Body = body,
            RecipientAddress = recipientAddress,
            EntityType = entityType,
            EntityId = entityId,
            CreatedBy = "System"
        };

        await _db.Notifications.AddAsync(notification, ct);
        await _db.SaveChangesAsync(ct);

        // TODO: Integrate with actual Email/SMS provider (e.g. SendGrid, Twilio, Egyptian telecom gateway)
        _logger.LogInformation("Notification queued: {Type} to {Recipient} for taxpayer {TaxpayerId}", type, recipientAddress, taxpayerId);
    }

    public Task SendBillIssuedAsync(Guid taxpayerId, Guid billId, decimal amount, CancellationToken ct = default) =>
        SendAsync(taxpayerId, NotificationType.BillIssued, NotificationChannel.Email,
            "Tax Bill Issued",
            $"A new tax bill of {amount:N2} EGP has been issued. Bill ID: {billId}",
            "TaxBill", billId, ct);

    public Task SendPaymentConfirmationAsync(Guid taxpayerId, Guid paymentId, decimal amount, CancellationToken ct = default) =>
        SendAsync(taxpayerId, NotificationType.PaymentReceived, NotificationChannel.Email,
            "Payment Received",
            $"Your payment of {amount:N2} EGP has been received. Reference: {paymentId}",
            "Payment", paymentId, ct);

    public Task SendAppealDecisionAsync(Guid taxpayerId, Guid appealId, string decision, CancellationToken ct = default) =>
        SendAsync(taxpayerId, NotificationType.AppealDecision, NotificationChannel.Email,
            $"Appeal Decision: {decision}",
            $"A decision has been made on your appeal. Decision: {decision}. Appeal ID: {appealId}",
            "Appeal", appealId, ct);
}
