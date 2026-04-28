using RealEstateTax.Domain.Enums;

namespace RealEstateTax.Application.DTOs.Valuations;

public class ValuationDto
{
    public Guid Id { get; set; }
    public string ValuationCode { get; set; } = string.Empty;
    public Guid PropertyId { get; set; }
    public string PropertyCode { get; set; } = string.Empty;
    public ValuationStatus Status { get; set; }
    public ValuationMethod Method { get; set; }
    public decimal GrossValue { get; set; }
    public decimal NetValue { get; set; }
    public decimal? DeductionPercentage { get; set; }
    public int TaxYear { get; set; }
    public DateTime ValuationDate { get; set; }
    public string? ApprovedBy { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class CreateValuationRequest
{
    public Guid PropertyId { get; set; }
    public ValuationMethod Method { get; set; } = ValuationMethod.RentalValue;
    public int TaxYear { get; set; }
    public DateTime ValuationDate { get; set; }
    public decimal TotalArea { get; set; }
    public decimal? RentableArea { get; set; }
    public decimal? AnnualRentalValue { get; set; }
    public decimal? MarketValuePerSqM { get; set; }
    public decimal? CapitalizationRate { get; set; }
    public Guid? ValuationRuleId { get; set; }
    public string? ComparableProperties { get; set; }
}

public class ApproveValuationRequest
{
    public string? ApprovalNotes { get; set; }
}

public class RejectValuationRequest
{
    public string RejectionReason { get; set; } = string.Empty;
}
