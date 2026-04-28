using RealEstateTax.Domain.Common;

namespace RealEstateTax.Domain.Entities;

/// <summary>
/// Immutable audit trail. Records are never updated or soft-deleted.
/// </summary>
public class AuditLog
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    public Guid? UserId { get; set; }
    public User? User { get; set; }
    public string? Username { get; set; }
    public string? UserRole { get; set; }
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }

    public string Action { get; set; } = string.Empty;     // CREATE, UPDATE, DELETE, APPROVE, etc.
    public string EntityType { get; set; } = string.Empty;
    public Guid? EntityId { get; set; }
    public string? EntityCode { get; set; }

    public string? OldValues { get; set; }                 // JSON snapshot before change
    public string? NewValues { get; set; }                 // JSON snapshot after change
    public string? ChangeSummary { get; set; }

    public string? CorrelationId { get; set; }
    public string? HttpMethod { get; set; }
    public string? RequestPath { get; set; }
    public bool IsSuccess { get; set; } = true;
    public string? FailureReason { get; set; }
}
