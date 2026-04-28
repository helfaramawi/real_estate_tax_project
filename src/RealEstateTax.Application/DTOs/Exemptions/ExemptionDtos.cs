using RealEstateTax.Domain.Enums;

namespace RealEstateTax.Application.DTOs.Exemptions;

public class ExemptionDto
{
    public Guid Id { get; set; }
    public string ExemptionCode { get; set; } = string.Empty;
    public Guid PropertyId { get; set; }
    public string PropertyCode { get; set; } = string.Empty;
    public Guid TaxpayerId { get; set; }
    public string TaxpayerName { get; set; } = string.Empty;
    public ExemptionType Type { get; set; }
    public ExemptionStatus Status { get; set; }
    public decimal? ExemptionPercentage { get; set; }
    public decimal? ExemptAmount { get; set; }
    public string JustificationSummary { get; set; } = string.Empty;
    public DateTime SubmittedAt { get; set; }
    public DateTime? EffectiveFrom { get; set; }
    public DateTime? EffectiveTo { get; set; }
    public string? ApprovedBy { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class SubmitExemptionRequest
{
    public Guid PropertyId { get; set; }
    public Guid TaxpayerId { get; set; }
    public ExemptionType Type { get; set; }
    public string JustificationSummary { get; set; } = string.Empty;
    public DateTime? EffectiveFrom { get; set; }
    public DateTime? EffectiveTo { get; set; }
    public Guid? ExemptionRuleId { get; set; }
}

public class ApproveExemptionRequest
{
    public decimal? ExemptionPercentage { get; set; }
    public decimal? ExemptAmount { get; set; }
    public string? ApprovalNotes { get; set; }
    public DateTime? EffectiveFrom { get; set; }
    public DateTime? EffectiveTo { get; set; }
}

public class RejectExemptionRequest
{
    public string RejectionReason { get; set; } = string.Empty;
}
