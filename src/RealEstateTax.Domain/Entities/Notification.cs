using RealEstateTax.Domain.Common;
using RealEstateTax.Domain.Enums;

namespace RealEstateTax.Domain.Entities;

public class Notification : BaseEntity
{
    public Guid TaxpayerId { get; set; }
    public Taxpayer Taxpayer { get; set; } = null!;

    public NotificationType Type { get; set; }
    public NotificationChannel Channel { get; set; }
    public NotificationStatus Status { get; set; } = NotificationStatus.Pending;

    public string Subject { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public string? TemplateId { get; set; }
    public string? TemplateData { get; set; }   // JSON parameters for template

    public string? RecipientAddress { get; set; }  // Email or phone number

    public DateTime? ScheduledAt { get; set; }
    public DateTime? SentAt { get; set; }
    public DateTime? DeliveredAt { get; set; }
    public DateTime? ReadAt { get; set; }

    public string? ProviderMessageId { get; set; }
    public string? FailureReason { get; set; }
    public int RetryCount { get; set; }

    // Reference to triggering entity
    public string? EntityType { get; set; }
    public Guid? EntityId { get; set; }
}
