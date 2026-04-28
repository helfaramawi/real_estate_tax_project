using RealEstateTax.Domain.Common;
using RealEstateTax.Domain.Enums;

namespace RealEstateTax.Domain.Entities;

public class TaxBill : BaseEntity
{
    public string BillNumber { get; set; } = string.Empty;
    public Guid PropertyId { get; set; }
    public Property Property { get; set; } = null!;

    public Guid TaxAssessmentId { get; set; }
    public TaxAssessment TaxAssessment { get; set; } = null!;

    public Guid TaxpayerId { get; set; }
    public Taxpayer Taxpayer { get; set; } = null!;

    public BillStatus Status { get; set; } = BillStatus.Draft;

    public int TaxYear { get; set; }
    public decimal TotalAmount { get; set; }                // EGP
    public decimal PaidAmount { get; set; }                 // EGP
    public decimal RemainingAmount => TotalAmount - PaidAmount;
    public decimal? DiscountAmount { get; set; }            // EGP — early payment discount
    public decimal? PenaltyAmount { get; set; }             // EGP — late payment penalty

    public DateTime IssueDate { get; set; }
    public DateTime DueDate { get; set; }
    // TODO: Configure installment schedule per Egyptian tax regulations
    public bool AllowsInstallments { get; set; }
    public int? NumberOfInstallments { get; set; }

    public DateTime? IssuedAt { get; set; }
    public string? IssuedBy { get; set; }
    public DateTime? CancelledAt { get; set; }
    public string? CancelledBy { get; set; }
    public string? CancellationReason { get; set; }

    public ICollection<Payment> Payments { get; set; } = [];
}
