using RealEstateTax.Domain.Enums;

namespace RealEstateTax.Application.DTOs.Payments;

public class PaymentDto
{
    public Guid Id { get; set; }
    public string PaymentReference { get; set; } = string.Empty;
    public Guid TaxBillId { get; set; }
    public string BillNumber { get; set; } = string.Empty;
    public Guid TaxpayerId { get; set; }
    public string TaxpayerName { get; set; } = string.Empty;
    public PaymentStatus Status { get; set; }
    public PaymentMethod Method { get; set; }
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "EGP";
    public DateTime? PaidAt { get; set; }
    public string? ReceiptNumber { get; set; }
    public string? ExternalTransactionId { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class RegisterPaymentRequest
{
    public Guid TaxBillId { get; set; }
    public PaymentMethod Method { get; set; }
    public decimal Amount { get; set; }
    public string? ExternalTransactionId { get; set; }
    public string? BankReference { get; set; }
    public DateTime? PaidAt { get; set; }
    public string? Notes { get; set; }
}
