using RealEstateTax.Domain.Common;
using RealEstateTax.Domain.Enums;

namespace RealEstateTax.Domain.Entities;

public class RiskScore : BaseEntity
{
    public Guid PropertyId { get; set; }
    public Property Property { get; set; } = null!;

    public RiskLevel Level { get; set; }
    public double Score { get; set; }               // 0–100

    // Component scores — each factor contributes to the total
    public double DataCompletenessScore { get; set; }
    public double ValuationConsistencyScore { get; set; }
    public double OwnershipChainScore { get; set; }
    public double PaymentHistoryScore { get; set; }
    public double GeoVerificationScore { get; set; }
    public double SourceConsistencyScore { get; set; }

    public string? RiskFactors { get; set; }        // JSON array of contributing factors
    public string? Recommendations { get; set; }    // JSON array of suggested actions

    public DateTime CalculatedAt { get; set; } = DateTime.UtcNow;
    public string? CalculatedBy { get; set; }       // "System" or user ID
    public int ModelVersion { get; set; } = 1;
    // TODO: Integrate with ML model for advanced risk scoring
}

public class FraudFlag : BaseEntity
{
    public Guid PropertyId { get; set; }
    public Property Property { get; set; } = null!;

    public FraudFlagType FlagType { get; set; }
    public FraudFlagStatus Status { get; set; } = FraudFlagStatus.Open;
    public RiskLevel Severity { get; set; }

    public string Description { get; set; } = string.Empty;
    public string? Evidence { get; set; }           // JSON details

    public Guid? RaisedByUserId { get; set; }
    public User? RaisedByUser { get; set; }
    public string? RaisedBySystem { get; set; }    // "AutoDetection", "ExternalSystem"

    public Guid? InvestigatedByUserId { get; set; }
    public User? InvestigatedByUser { get; set; }
    public DateTime? InvestigationStartedAt { get; set; }
    public DateTime? ResolvedAt { get; set; }
    public string? ResolutionNotes { get; set; }
}
