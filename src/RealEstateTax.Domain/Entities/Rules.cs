using RealEstateTax.Domain.Common;

namespace RealEstateTax.Domain.Entities;

/// <summary>
/// Configurable tax rules — rates are NOT hard-coded.
/// TODO: Populate with verified values from Egyptian Real Estate Tax Law 196/2008 and subsequent ministerial decrees.
/// </summary>
public class TaxRule : BaseEntity
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string PropertyType { get; set; } = string.Empty;
    public string? Governorate { get; set; }            // null = applies to all
    public decimal TaxRate { get; set; }                // % e.g. 10.00
    public decimal? MinTaxableValue { get; set; }       // EGP — threshold below which no tax
    public decimal? MaxTaxableValue { get; set; }       // EGP — bracket ceiling
    public decimal? FlatAmount { get; set; }            // EGP — for fixed-rate scenarios
    public int? ApplicableFromYear { get; set; }
    public int? ApplicableToYear { get; set; }
    public bool IsActive { get; set; } = true;
    public string? LegalReference { get; set; }
    // TODO: Add bracketed tax rate schedule once Egyptian tax authority confirms current rates
}

/// <summary>
/// Configurable valuation rules for determining taxable value.
/// TODO: Verify rental value deduction rates per Egyptian tax regulations.
/// </summary>
public class ValuationRule : BaseEntity
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? PropertyType { get; set; }
    public string? Governorate { get; set; }
    public decimal DeductionPercentage { get; set; }    // % maintenance/depreciation deduction
    public decimal? MinNetValue { get; set; }
    public decimal? MaxNetValue { get; set; }
    public bool UseRentalValue { get; set; } = true;   // Egyptian law uses rental value as base
    public decimal? StandardRentPerSqM { get; set; }   // EGP/m²/year
    public int? ApplicableFromYear { get; set; }
    public bool IsActive { get; set; } = true;
    public string? LegalReference { get; set; }
}

/// <summary>
/// Configurable exemption eligibility rules.
/// TODO: Map to specific articles of Egyptian Real Estate Tax Law 196/2008.
/// </summary>
public class ExemptionRule : BaseEntity
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string ExemptionType { get; set; } = string.Empty;
    public decimal? ExemptionPercentage { get; set; }   // % of tax exempt
    public bool IsFullExemption { get; set; }
    public decimal? MaxExemptAmount { get; set; }       // EGP cap
    public string? EligibilityCriteria { get; set; }   // JSON rules
    public bool RequiresApproval { get; set; } = true;
    public int? MaxDurationMonths { get; set; }
    public bool IsActive { get; set; } = true;
    public string? LegalReference { get; set; }
}

public class DataQualityIssue : BaseEntity
{
    public Guid PropertyId { get; set; }
    public Property Property { get; set; } = null!;

    public string IssueType { get; set; } = string.Empty;
    public string Severity { get; set; } = string.Empty;
    public string Status { get; set; } = "Open";

    public string Description { get; set; } = string.Empty;
    public string? AffectedField { get; set; }
    public string? SourceSystemCode { get; set; }
    public string? SuggestedAction { get; set; }

    public DateTime? ResolvedAt { get; set; }
    public string? ResolvedBy { get; set; }
    public string? ResolutionNotes { get; set; }
}
