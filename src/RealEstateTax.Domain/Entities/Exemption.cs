using RealEstateTax.Domain.Common;
using RealEstateTax.Domain.Enums;

namespace RealEstateTax.Domain.Entities;

public class Exemption : BaseEntity
{
    public string ExemptionCode { get; set; } = string.Empty;
    public Guid PropertyId { get; set; }
    public Property Property { get; set; } = null!;

    public Guid TaxpayerId { get; set; }
    public Taxpayer Taxpayer { get; set; } = null!;

    public ExemptionType Type { get; set; }
    public ExemptionStatus Status { get; set; } = ExemptionStatus.Submitted;

    public decimal? ExemptionPercentage { get; set; }   // 0–100 partial exemption
    public decimal? ExemptAmount { get; set; }          // Fixed EGP amount (alternative to %)

    public string JustificationSummary { get; set; } = string.Empty;
    public string? SupportingDocuments { get; set; }    // JSON list of document references

    public DateTime SubmittedAt { get; set; } = DateTime.UtcNow;
    public DateTime? EffectiveFrom { get; set; }
    public DateTime? EffectiveTo { get; set; }          // null = indefinite

    // Approval (Maker-Checker)
    public string? ReviewedBy { get; set; }
    public DateTime? ReviewedAt { get; set; }
    public string? ApprovedBy { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public string? ApprovalNotes { get; set; }
    public string? RejectionReason { get; set; }
    public string? RevocationReason { get; set; }

    public Guid? ExemptionRuleId { get; set; }
    public ExemptionRule? ExemptionRule { get; set; }
    // TODO: Map exemption types to specific articles of Egyptian Real Estate Tax Law 196/2008
}
