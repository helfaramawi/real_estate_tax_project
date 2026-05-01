using RealEstateTax.Intelligence.Domain.Enums;

namespace RealEstateTax.Intelligence.Application.DTOs;

public record PredictionRequestDto(
    string ModelName,
    string ModelVersion,
    List<FeatureRowDto> Features
);

public record FeatureRowDto(
    string PropertyId,
    double? Lat,
    double? Lon,
    double? BuiltUpArea,
    double? LandArea,
    int? PropertyTypeCode,
    int? YearBuilt,
    double? DeclaredAnnualValue,
    double? MarketValuePerSqm,
    double? PaidOnTimeRate,
    int? OverdueCount,
    double? NearestNeighborDistanceM,
    int? NeighborsWithin100m,
    int? ExistingRiskScore,
    int? FraudFlagsCount,
    bool? CorporateOwnerFlag,
    int? OwnershipChainLength,
    int? DaysSinceLastSurvey,
    int? SurveysCount,
    double? ValueVsClusterMedianPct
);

public record PredictionResponseDto(
    string PropertyId,
    double Score,
    string Label,
    double Confidence,
    Dictionary<string, double> Explanation
);

public class PagedPredictionsDto
{
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public List<PredictionItemDto> Items { get; set; } = [];
}

public class PredictionItemDto
{
    public Guid Id { get; set; }
    public Guid PropertyId { get; set; }
    public string PredictionType { get; set; } = string.Empty;
    public double Score { get; set; }
    public string? Label { get; set; }
    public double Confidence { get; set; }
    public Dictionary<string, double>? Explanation { get; set; }
    public DateTime PredictedAt { get; set; }
    public bool IsReviewed { get; set; }
}

public class IntelligenceSummaryDto
{
    public int TotalPredictions { get; set; }
    public int PendingReview { get; set; }
    public int HighRiskCount { get; set; }
    public int FraudSuspectCount { get; set; }
    public int OpenAnomalies { get; set; }
    public int CriticalAnomalies { get; set; }
    public List<ModelSummaryDto> ActiveModels { get; set; } = [];
}

public class ModelSummaryDto
{
    public Guid Id { get; set; }
    public string ModelName { get; set; } = string.Empty;
    public string Version { get; set; } = string.Empty;
    public string PredictionType { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime? TrainedAt { get; set; }
    public Dictionary<string, double>? Metrics { get; set; }
}
