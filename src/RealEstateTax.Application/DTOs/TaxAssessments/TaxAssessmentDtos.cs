using RealEstateTax.Domain.Enums;

namespace RealEstateTax.Application.DTOs.TaxAssessments;

public class TaxAssessmentDto
{
    public Guid Id { get; set; }
    public string AssessmentCode { get; set; } = string.Empty;
    public Guid PropertyId { get; set; }
    public string PropertyCode { get; set; } = string.Empty;
    public Guid ValuationId { get; set; }
    public AssessmentStatus Status { get; set; }
    public int TaxYear { get; set; }
    public decimal TaxableValue { get; set; }
    public decimal TaxRate { get; set; }
    public decimal GrossTaxAmount { get; set; }
    public decimal? ExemptionAmount { get; set; }
    public decimal NetTaxAmount { get; set; }
    public decimal TotalDue { get; set; }
    public string? ApprovedBy { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class GenerateTaxAssessmentRequest
{
    public Guid PropertyId { get; set; }
    public Guid ValuationId { get; set; }
    public int TaxYear { get; set; }
    public Guid? TaxRuleId { get; set; }
    public Guid? ExemptionId { get; set; }
}

public class ApproveTaxAssessmentRequest
{
    public string? ApprovalNotes { get; set; }
}
