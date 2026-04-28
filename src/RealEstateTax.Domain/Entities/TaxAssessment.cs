using RealEstateTax.Domain.Common;
using RealEstateTax.Domain.Enums;

namespace RealEstateTax.Domain.Entities;

public class TaxAssessment : BaseEntity
{
    public string AssessmentCode { get; set; } = string.Empty;
    public Guid PropertyId { get; set; }
    public Property Property { get; set; } = null!;

    public Guid ValuationId { get; set; }
    public Valuation Valuation { get; set; } = null!;

    public AssessmentStatus Status { get; set; } = AssessmentStatus.Draft;

    public int TaxYear { get; set; }
    public decimal TaxableValue { get; set; }               // EGP
    public decimal TaxRate { get; set; }                    // %
    // TODO: Confirm applicable rate per Egyptian Real Estate Tax Law and annual decrees
    public decimal GrossTaxAmount { get; set; }             // EGP
    public decimal? ExemptionAmount { get; set; }           // EGP
    public decimal NetTaxAmount { get; set; }               // EGP
    public decimal? Penalties { get; set; }                 // EGP
    public decimal? LateFees { get; set; }                  // EGP
    public decimal TotalDue { get; set; }                   // EGP = NetTaxAmount + Penalties + LateFees

    public Guid? TaxRuleId { get; set; }
    public TaxRule? TaxRule { get; set; }

    public Guid? ExemptionId { get; set; }
    public Exemption? Exemption { get; set; }

    // Approval (Maker-Checker)
    public string? PreparedBy { get; set; }
    public DateTime? PreparedAt { get; set; }
    public string? ApprovedBy { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public string? ApprovalNotes { get; set; }

    public ICollection<TaxBill> TaxBills { get; set; } = [];
}
