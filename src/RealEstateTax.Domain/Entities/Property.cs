using RealEstateTax.Domain.Common;
using RealEstateTax.Domain.Enums;

namespace RealEstateTax.Domain.Entities;

public class Property : BaseEntity
{
    public string PropertyCode { get; set; } = string.Empty;   // Master property identifier
    public string? ParcelNumber { get; set; }
    public string? CadastralReference { get; set; }

    // Classification
    public PropertyType Type { get; set; }
    public PropertyStatus Status { get; set; } = PropertyStatus.Draft;
    public string? SubType { get; set; }
    public string? UsageDescription { get; set; }

    // Physical attributes
    public decimal BuiltUpArea { get; set; }           // m²
    public decimal? LandArea { get; set; }             // m²
    public int? NumberOfFloors { get; set; }
    public int? FloorNumber { get; set; }
    public int? NumberOfUnits { get; set; }
    public int? YearBuilt { get; set; }
    public int? YearLastRenovated { get; set; }
    public string? BuildingCondition { get; set; }

    // Address
    public string? StreetAddress { get; set; }
    public string? BuildingNumber { get; set; }
    public string? Neighbourhood { get; set; }
    public string? District { get; set; }
    public string? City { get; set; }
    public string? Governorate { get; set; }
    public string? PostalCode { get; set; }
    public string? FullAddress { get; set; }

    // Duplicate detection & quality
    public double ConfidenceScore { get; set; } = 1.0;
    public bool HasDuplicateSuspicion { get; set; }
    public Guid? MergedIntoPropertyId { get; set; }

    // Approval workflow (Maker-Checker)
    public string? VerifiedBy { get; set; }
    public DateTime? VerifiedAt { get; set; }
    public string? VerificationNotes { get; set; }
    public string? ReviewNotes { get; set; }

    public ICollection<PropertyUnit> Units { get; set; } = [];
    public ICollection<PropertyOwnership> Ownerships { get; set; } = [];
    public PropertyLocation? Location { get; set; }
    public ICollection<PropertySourceRecord> SourceRecords { get; set; } = [];
    public ICollection<FieldSurvey> FieldSurveys { get; set; } = [];
    public ICollection<Valuation> Valuations { get; set; } = [];
    public ICollection<TaxAssessment> TaxAssessments { get; set; } = [];
    public ICollection<TaxBill> TaxBills { get; set; } = [];
    public ICollection<Exemption> Exemptions { get; set; } = [];
    public ICollection<Appeal> Appeals { get; set; } = [];
    public ICollection<RiskScore> RiskScores { get; set; } = [];
    public ICollection<FraudFlag> FraudFlags { get; set; } = [];
    public ICollection<DataQualityIssue> DataQualityIssues { get; set; } = [];
}
