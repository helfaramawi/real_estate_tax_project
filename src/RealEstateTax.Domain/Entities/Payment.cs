using RealEstateTax.Domain.Common;
using RealEstateTax.Domain.Enums;

namespace RealEstateTax.Domain.Entities;

public class Payment : BaseEntity
{
    public string PaymentReference { get; set; } = string.Empty;
    public Guid TaxBillId { get; set; }
    public TaxBill TaxBill { get; set; } = null!;

    public Guid TaxpayerId { get; set; }
    public Taxpayer Taxpayer { get; set; } = null!;

    public PaymentStatus Status { get; set; } = PaymentStatus.Pending;
    public PaymentMethod Method { get; set; }

    public decimal Amount { get; set; }             // EGP
    public decimal? FeeAmount { get; set; }         // EGP — processing fee
    public string Currency { get; set; } = "EGP";

    public DateTime? PaidAt { get; set; }
    public string? ExternalTransactionId { get; set; }
    public string? BankReference { get; set; }
    public string? ReceiptNumber { get; set; }
    public string? CollectedBy { get; set; }        // Officer ID for cash payments

    public string? FailureReason { get; set; }
    public DateTime? RefundedAt { get; set; }
    public string? RefundReason { get; set; }

    public string? Notes { get; set; }
}
