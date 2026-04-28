using RealEstateTax.Domain.Common;
using RealEstateTax.Domain.Enums;

namespace RealEstateTax.Domain.Entities;

public class Valuation : BaseEntity
{
    public string ValuationCode { get; set; } = string.Empty;
    public Guid PropertyId { get; set; }
    public Property Property { get; set; } = null!;

    public ValuationStatus Status { get; set; } = ValuationStatus.Draft;
    public ValuationMethod Method { get; set; }

    // Valuation inputs
    public decimal TotalArea { get; set; }                  // m²
    public decimal? RentableArea { get; set; }              // m²
    public decimal? AnnualRentalValue { get; set; }         // EGP — for rental value method
    public decimal? MarketValuePerSqM { get; set; }         // EGP/m²
    public decimal? CapitalizationRate { get; set; }        // % — for income method
    public string? ComparableProperties { get; set; }       // JSON array of comparable refs

    // Output
    public decimal GrossValue { get; set; }                 // EGP
    public decimal? DeductionPercentage { get; set; }       // % maintenance deduction
    // TODO: Verify deduction rate per Egyptian Real Estate Tax Law 196/2008 and amendments
    public decimal NetValue { get; set; }                   // EGP — taxable base

    public int TaxYear { get; set; }
    public DateTime ValuationDate { get; set; }
    public DateTime? ValidUntil { get; set; }

    // Approval (Maker-Checker)
    public string? PreparedBy { get; set; }
    public DateTime? PreparedAt { get; set; }
    public string? ApprovedBy { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public string? ApprovalNotes { get; set; }
    public string? RejectionReason { get; set; }

    public Guid? ValuationRuleId { get; set; }
    public ValuationRule? ValuationRule { get; set; }

    public ICollection<TaxAssessment> TaxAssessments { get; set; } = [];
}
