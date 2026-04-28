using RealEstateTax.Domain.Enums;

namespace RealEstateTax.Application.DTOs.Bills;

public class TaxBillDto
{
    public Guid Id { get; set; }
    public string BillNumber { get; set; } = string.Empty;
    public Guid PropertyId { get; set; }
    public string PropertyCode { get; set; } = string.Empty;
    public string? PropertyAddress { get; set; }
    public Guid TaxpayerId { get; set; }
    public string TaxpayerName { get; set; } = string.Empty;
    public BillStatus Status { get; set; }
    public int TaxYear { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal PaidAmount { get; set; }
    public decimal RemainingAmount { get; set; }
    public decimal? PenaltyAmount { get; set; }
    public DateTime IssueDate { get; set; }
    public DateTime DueDate { get; set; }
    public DateTime? IssuedAt { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class GenerateBillRequest
{
    public Guid TaxAssessmentId { get; set; }
    public DateTime DueDate { get; set; }
    public bool AllowsInstallments { get; set; }
    public int? NumberOfInstallments { get; set; }
}

public class IssueBillRequest
{
    public DateTime? OverrideDueDate { get; set; }
}

public class CancelBillRequest
{
    public string CancellationReason { get; set; } = string.Empty;
}
