using RealEstateTax.Domain.Common;
using RealEstateTax.Domain.Enums;

namespace RealEstateTax.Domain.Entities;

public class ExternalEntity : BaseEntity
{
    public string Code { get; set; } = string.Empty;           // e.g. "NILE_WATER", "GECOL"
    public string Name { get; set; } = string.Empty;           // Full name
    public string? ShortName { get; set; }
    public string? Description { get; set; }
    public string? BaseUrl { get; set; }
    public string? AuthType { get; set; }                       // ApiKey, OAuth2, Basic
    public string? ApiKeyHeader { get; set; }
    public string? ApiKeyValue { get; set; }                    // Encrypted at rest
    public bool IsActive { get; set; } = true;
    public int TimeoutSeconds { get; set; } = 30;
    public int MaxRetries { get; set; } = 3;

    public ICollection<IntegrationRequest> IntegrationRequests { get; set; } = [];
}

public class IntegrationRequest : BaseEntity
{
    public Guid ExternalEntityId { get; set; }
    public ExternalEntity ExternalEntity { get; set; } = null!;

    public IntegrationDirection Direction { get; set; }
    public IntegrationRequestStatus Status { get; set; } = IntegrationRequestStatus.Queued;

    public string OperationType { get; set; } = string.Empty;  // PROPERTY_LOOKUP, DATA_SYNC, etc.
    public string? RequestPayload { get; set; }                 // JSON
    public string? ResponsePayload { get; set; }                // JSON
    public int HttpStatusCode { get; set; }

    public DateTime QueuedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ProcessedAt { get; set; }
    public string? ErrorMessage { get; set; }
    public int RetryCount { get; set; }
    public DateTime? NextRetryAt { get; set; }

    public string? CorrelationId { get; set; }

    // Linked entity
    public string? RelatedEntityType { get; set; }
    public Guid? RelatedEntityId { get; set; }
}
