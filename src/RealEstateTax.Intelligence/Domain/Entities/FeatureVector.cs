namespace RealEstateTax.Intelligence.Domain.Entities;

public class FeatureVector
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid PropertyId { get; set; }
    public DateTime ComputedAt { get; set; } = DateTime.UtcNow;
    public string FeatureVersion { get; set; } = "v1";

    // Spatial features
    public double? Lat { get; set; }
    public double? Lon { get; set; }
    public bool HasBoundaryPolygon { get; set; }
    public double? BoundaryAreaSqm { get; set; }
    public double? NearestNeighborDistanceM { get; set; }
    public int? NeighborsWithin100m { get; set; }
    public int? NeighborsWithin500m { get; set; }
    public bool InGeoCluster { get; set; }
    public Guid? ClusterId { get; set; }
    public double? DistanceToClusterCenterM { get; set; }

    // Property attributes
    public double? BuiltUpArea { get; set; }
    public double? LandArea { get; set; }
    public int? PropertyTypeCode { get; set; }
    public int? YearBuilt { get; set; }
    public int? FloorsCount { get; set; }
    public int? UnitsCount { get; set; }

    // Valuation features
    public double? DeclaredAnnualValue { get; set; }
    public double? MarketValuePerSqm { get; set; }
    public double? CapitalizationRate { get; set; }
    public double? ValueVsClusterMedianPct { get; set; }
    public double? ValueVsDistrictMedianPct { get; set; }

    // Ownership features
    public int? OwnershipChainLength { get; set; }
    public int? DaysSinceLastTransfer { get; set; }
    public int? TransferCount3y { get; set; }
    public bool MultipleOwnersFlag { get; set; }
    public bool CorporateOwnerFlag { get; set; }

    // Survey features
    public int? SurveysCount { get; set; }
    public int? DaysSinceLastSurvey { get; set; }
    public double? GpsAccuracyAvg { get; set; }
    public double? MeasuredVsDeclaredAreaDeltaPct { get; set; }

    // Payment / billing features
    public int? BillsCount { get; set; }
    public double? PaidOnTimeRate { get; set; }
    public int? OverdueCount { get; set; }
    public double? TotalPaidEgp { get; set; }
    public double? TotalOutstandingEgp { get; set; }

    // Risk / compliance features
    public int? ExistingRiskScore { get; set; }
    public int? GeoVerificationScore { get; set; }
    public int? FraudFlagsCount { get; set; }
    public int? AppealsCount { get; set; }
    public int? ExemptionsCount { get; set; }

    // Source matching features
    public int? SourceRecordsCount { get; set; }
    public int? MatchedRecordsCount { get; set; }
    public double? MaxMatchConfidence { get; set; }
    public int? SourceSystemsCount { get; set; }
}
