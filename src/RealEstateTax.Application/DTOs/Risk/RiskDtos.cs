using RealEstateTax.Domain.Enums;

namespace RealEstateTax.Application.DTOs.Risk;

public class RiskScoreDto
{
    public Guid Id { get; set; }
    public Guid PropertyId { get; set; }
    public string PropertyCode { get; set; } = string.Empty;
    public RiskLevel Level { get; set; }
    public double Score { get; set; }
    public double DataCompletenessScore { get; set; }
    public double ValuationConsistencyScore { get; set; }
    public double OwnershipChainScore { get; set; }
    public double PaymentHistoryScore { get; set; }
    public double GeoVerificationScore { get; set; }
    public double SourceConsistencyScore { get; set; }
    public List<string> RiskFactors { get; set; } = [];
    public List<string> Recommendations { get; set; } = [];
    public DateTime CalculatedAt { get; set; }
}

public class FraudFlagDto
{
    public Guid Id { get; set; }
    public Guid PropertyId { get; set; }
    public string PropertyCode { get; set; } = string.Empty;
    public FraudFlagType FlagType { get; set; }
    public FraudFlagStatus Status { get; set; }
    public RiskLevel Severity { get; set; }
    public string Description { get; set; } = string.Empty;
    public string? Evidence { get; set; }
    public string? RaisedBySystem { get; set; }
    public string? RaisedByName { get; set; }
    public DateTime? ResolvedAt { get; set; }
    public string? ResolutionNotes { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class CreateFraudFlagRequest
{
    public Guid PropertyId { get; set; }
    public FraudFlagType FlagType { get; set; }
    public RiskLevel Severity { get; set; }
    public string Description { get; set; } = string.Empty;
    public string? Evidence { get; set; }
}
