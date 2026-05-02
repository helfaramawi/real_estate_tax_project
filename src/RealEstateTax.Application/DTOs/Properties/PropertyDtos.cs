using RealEstateTax.Domain.Enums;

namespace RealEstateTax.Application.DTOs.Properties;

public class PropertyDto
{
    public Guid Id { get; set; }
    public string PropertyCode { get; set; } = string.Empty;
    public string? ParcelNumber { get; set; }
    public PropertyType Type { get; set; }
    public string TypeName { get; set; } = string.Empty;
    public PropertyStatus Status { get; set; }
    public string StatusName { get; set; } = string.Empty;
    public decimal BuiltUpArea { get; set; }
    public decimal? LandArea { get; set; }
    public int? YearBuilt { get; set; }
    public string? StreetAddress { get; set; }
    public string? Neighbourhood { get; set; }
    public string? District { get; set; }
    public string? City { get; set; }
    public string? Governorate { get; set; }
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    public double ConfidenceScore { get; set; }
    public bool HasDuplicateSuspicion { get; set; }
    public DateTime CreatedAt { get; set; }
    public string? PrimaryOwnerName { get; set; }
    public decimal? CurrentAssessedValue { get; set; }
    public decimal? CurrentTaxAmount { get; set; }
}

public class PropertyDetailDto : PropertyDto
{
    public string? CadastralReference { get; set; }
    public string? SubType { get; set; }
    public string? UsageDescription { get; set; }
    public int? NumberOfFloors { get; set; }
    public int? FloorNumber { get; set; }
    public int? NumberOfUnits { get; set; }
    public int? YearLastRenovated { get; set; }
    public string? BuildingCondition { get; set; }
    public string? BuildingNumber { get; set; }
    public string? PostalCode { get; set; }
    public string? VerifiedBy { get; set; }
    public DateTime? VerifiedAt { get; set; }
    public string? VerificationNotes { get; set; }
    public PropertyLocationDto? Location { get; set; }
    public List<PropertyOwnershipDto> Ownerships { get; set; } = [];
    public List<PropertyUnitDto> Units { get; set; } = [];
    public ValuationSummaryDto? LatestValuation { get; set; }
    public TaxAssessmentSummaryDto? LatestAssessment { get; set; }
    public TaxBillSummaryDto? LatestBill { get; set; }
    public RiskScoreSummaryDto? RiskScore { get; set; }
    // ML Intelligence scores from the nightly batch inference pipeline
    public double? MlRiskScore { get; set; }
    public double? MlFraudProbability { get; set; }
    public double? MlDuplicateScore { get; set; }
    public DateTime? MlLastScoredAt { get; set; }
    public string? MlModelVersion { get; set; }
}

public class CreatePropertyRequest
{
    public PropertyType Type { get; set; }
    public string? SubType { get; set; }
    public string? ParcelNumber { get; set; }
    public string? CadastralReference { get; set; }
    public decimal BuiltUpArea { get; set; }
    public decimal? LandArea { get; set; }
    public int? NumberOfFloors { get; set; }
    public int? FloorNumber { get; set; }
    public int? YearBuilt { get; set; }
    public string? StreetAddress { get; set; }
    public string? BuildingNumber { get; set; }
    public string? Neighbourhood { get; set; }
    public string? District { get; set; }
    public string? City { get; set; }
    public string? Governorate { get; set; }
    public string? PostalCode { get; set; }
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    public string? UsageDescription { get; set; }
    public string? BuildingCondition { get; set; }
}

public class UpdatePropertyRequest : CreatePropertyRequest
{
    public string? ReviewNotes { get; set; }
}

public class VerifyPropertyRequest
{
    public string? VerificationNotes { get; set; }
}

public class LinkOwnerRequest
{
    public Guid TaxpayerId { get; set; }
    public OwnershipType OwnershipType { get; set; }
    public decimal OwnershipPercentage { get; set; } = 100m;
    public DateTime OwnershipStartDate { get; set; }
    public string? TitleDeedNumber { get; set; }
    public DateTime? TitleDeedDate { get; set; }
    public string? RegistrationAuthority { get; set; }
    public Guid? PropertyUnitId { get; set; }
}

public class PropertyLocationDto
{
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    public string? Governorate { get; set; }
    public string? Markaz { get; set; }
    public string? City { get; set; }
    public string? Neighbourhood { get; set; }
    public string? Block { get; set; }
    public string? GeocodingSource { get; set; }
    public double? GeocodingAccuracy { get; set; }
    public bool IsVerified { get; set; }
}

public class PropertyOwnershipDto
{
    public Guid Id { get; set; }
    public Guid TaxpayerId { get; set; }
    public string TaxpayerName { get; set; } = string.Empty;
    public string TaxpayerNationalId { get; set; } = string.Empty;
    public OwnershipType OwnershipType { get; set; }
    public decimal OwnershipPercentage { get; set; }
    public DateTime OwnershipStartDate { get; set; }
    public DateTime? OwnershipEndDate { get; set; }
    public bool IsCurrent { get; set; }
    public string? TitleDeedNumber { get; set; }
    public bool IsVerified { get; set; }
}

public class PropertyUnitDto
{
    public Guid Id { get; set; }
    public string UnitCode { get; set; } = string.Empty;
    public string? UnitNumber { get; set; }
    public int FloorNumber { get; set; }
    public decimal Area { get; set; }
    public string? UnitType { get; set; }
    public bool IsOccupied { get; set; }
    public bool IsRented { get; set; }
    public decimal? MonthlyRent { get; set; }
}

public class PropertyTimelineEventDto
{
    public DateTime Timestamp { get; set; }
    public string EventType { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string? PerformedBy { get; set; }
    public string? EntityType { get; set; }
    public Guid? EntityId { get; set; }
}

public class NearbyPropertyDto
{
    public Guid Id { get; set; }
    public string PropertyCode { get; set; } = string.Empty;
    public PropertyType Type { get; set; }
    public PropertyStatus Status { get; set; }
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public double DistanceMeters { get; set; }
    public string? Address { get; set; }
}

// Summary DTOs for nested contexts
public class ValuationSummaryDto
{
    public Guid Id { get; set; }
    public decimal NetValue { get; set; }
    public int TaxYear { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime ValuationDate { get; set; }
}

public class TaxAssessmentSummaryDto
{
    public Guid Id { get; set; }
    public decimal NetTaxAmount { get; set; }
    public int TaxYear { get; set; }
    public string Status { get; set; } = string.Empty;
}

public class TaxBillSummaryDto
{
    public Guid Id { get; set; }
    public string BillNumber { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }
    public decimal RemainingAmount { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime DueDate { get; set; }
}

public class RiskScoreSummaryDto
{
    public double Score { get; set; }
    public string Level { get; set; } = string.Empty;
    public DateTime CalculatedAt { get; set; }
}

public class DeletePropertyRequest
{
    public string Reason { get; set; } = string.Empty;
}

public class BulkImportRowError
{
    public int Row { get; set; }
    public string Message { get; set; } = string.Empty;
}

public class BulkImportResult
{
    public int Succeeded { get; set; }
    public int Failed { get; set; }
    public List<BulkImportRowError> Errors { get; set; } = [];
}
