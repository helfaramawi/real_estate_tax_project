using RealEstateTax.Domain.Enums;

namespace RealEstateTax.Application.DTOs.Integrations;

public class ReceiveIntegrationDataRequest
{
    public string OperationType { get; set; } = string.Empty;
    public string Payload { get; set; } = string.Empty;  // JSON string
    public string? CorrelationId { get; set; }
    public string? RelatedEntityType { get; set; }
    public Guid? RelatedEntityId { get; set; }
}

public class IntegrationRequestDto
{
    public Guid Id { get; set; }
    public string ExternalEntityCode { get; set; } = string.Empty;
    public string ExternalEntityName { get; set; } = string.Empty;
    public IntegrationDirection Direction { get; set; }
    public IntegrationRequestStatus Status { get; set; }
    public string OperationType { get; set; } = string.Empty;
    public int HttpStatusCode { get; set; }
    public DateTime QueuedAt { get; set; }
    public DateTime? ProcessedAt { get; set; }
    public string? ErrorMessage { get; set; }
    public int RetryCount { get; set; }
    public string? CorrelationId { get; set; }
}

public class IntegrationReceiveResultDto
{
    public Guid RequestId { get; set; }
    public bool Accepted { get; set; }
    public string? Message { get; set; }
    public string? CorrelationId { get; set; }
}
