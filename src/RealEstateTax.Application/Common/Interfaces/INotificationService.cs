using RealEstateTax.Domain.Enums;

namespace RealEstateTax.Application.Common.Interfaces;

public interface INotificationService
{
    Task SendAsync(
        Guid taxpayerId,
        NotificationType type,
        NotificationChannel channel,
        string subject,
        string body,
        string? entityType = null,
        Guid? entityId = null,
        CancellationToken ct = default);

    Task SendBillIssuedAsync(Guid taxpayerId, Guid billId, decimal amount, CancellationToken ct = default);
    Task SendPaymentConfirmationAsync(Guid taxpayerId, Guid paymentId, decimal amount, CancellationToken ct = default);
    Task SendAppealDecisionAsync(Guid taxpayerId, Guid appealId, string decision, CancellationToken ct = default);
}
