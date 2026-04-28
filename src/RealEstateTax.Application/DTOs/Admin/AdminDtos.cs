namespace RealEstateTax.Application.DTOs.Admin;

public class AuditLogDto
{
    public Guid Id { get; set; }
    public DateTime Timestamp { get; set; }
    public string? Username { get; set; }
    public string? UserRole { get; set; }
    public string? IpAddress { get; set; }
    public string Action { get; set; } = string.Empty;
    public string EntityType { get; set; } = string.Empty;
    public Guid? EntityId { get; set; }
    public string? EntityCode { get; set; }
    public string? ChangeSummary { get; set; }
    public string? CorrelationId { get; set; }
    public bool IsSuccess { get; set; }
    public string? FailureReason { get; set; }
}

public class DashboardKpiDto
{
    public int TotalProperties { get; set; }
    public int PropertiesDraft { get; set; }
    public int PropertiesVerified { get; set; }
    public int PropertiesTaxable { get; set; }
    public int PropertiesExempt { get; set; }
    public int TotalTaxpayers { get; set; }
    public int ActiveBills { get; set; }
    public decimal TotalBilledThisYear { get; set; }
    public decimal TotalCollectedThisYear { get; set; }
    public decimal CollectionRate { get; set; }
    public int PendingAppeals { get; set; }
    public int PendingExemptions { get; set; }
    public int OpenFraudFlags { get; set; }
    public int HighRiskProperties { get; set; }
    public int UnmatchedSourceRecords { get; set; }
    public int DataQualityIssues { get; set; }
    public DateTime AsOf { get; set; } = DateTime.UtcNow;
}
