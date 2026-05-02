using RealEstateTax.Intelligence.Domain.Enums;

namespace RealEstateTax.Intelligence.Application.DTOs;

public record PredictionRequestDto(
    string ModelName,
    string ModelVersion,
    List<FeatureRowDto> Features
);

// All 35 features — matches Python FeatureRow exactly.
// MLModelHttpClient uses JsonNamingPolicy.SnakeCaseLower so PascalCase
// properties are serialized as snake_case (e.g. BuiltUpArea → built_up_area).
public class FeatureRowDto
{
    public required string PropertyId { get; init; }

    // Spatial
    public double? Lat { get; init; }
    public double? Lon { get; init; }
    public bool? HasBoundaryPolygon { get; init; }
    public double? NearestNeighborDistanceM { get; init; }
    public int? NeighborsWithin100m { get; init; }
    public int? NeighborsWithin500m { get; init; }

    // Property attributes
    public double? BuiltUpArea { get; init; }
    public double? LandArea { get; init; }
    public int? PropertyTypeCode { get; init; }
    public int? YearBuilt { get; init; }

    // Valuation
    public double? DeclaredAnnualValue { get; init; }
    public double? MarketValuePerSqm { get; init; }
    public double? CapitalizationRate { get; init; }
    public double? ValueVsClusterMedianPct { get; init; }
    public double? ValueVsDistrictMedianPct { get; init; }

    // Ownership
    public int? OwnershipChainLength { get; init; }
    public int? DaysSinceLastTransfer { get; init; }
    public bool? CorporateOwnerFlag { get; init; }
    public bool? MultipleOwnersFlag { get; init; }

    // Survey
    public int? SurveysCount { get; init; }
    public int? DaysSinceLastSurvey { get; init; }
    public double? GpsAccuracyAvg { get; init; }

    // Payment / billing
    public int? BillsCount { get; init; }
    public double? PaidOnTimeRate { get; init; }
    public int? OverdueCount { get; init; }
    public double? TotalPaidEgp { get; init; }
    public double? TotalOutstandingEgp { get; init; }

    // Risk / compliance
    public int? ExistingRiskScore { get; init; }
    public int? GeoVerificationScore { get; init; }
    public int? FraudFlagsCount { get; init; }
    public int? AppealsCount { get; init; }
    public int? ExemptionsCount { get; init; }

    // Source matching
    public int? SourceRecordsCount { get; init; }
    public int? MatchedRecordsCount { get; init; }
    public double? MaxMatchConfidence { get; init; }
}

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
