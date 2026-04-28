using RealEstateTax.Domain.Enums;

namespace RealEstateTax.Application.DTOs.Appeals;

public class AppealDto
{
    public Guid Id { get; set; }
    public string AppealCode { get; set; } = string.Empty;
    public Guid PropertyId { get; set; }
    public string PropertyCode { get; set; } = string.Empty;
    public Guid TaxpayerId { get; set; }
    public string TaxpayerName { get; set; } = string.Empty;
    public AppealStatus Status { get; set; }
    public string GroundsSummary { get; set; } = string.Empty;
    public decimal? RequestedAssessmentValue { get; set; }
    public DateTime SubmittedAt { get; set; }
    public DateTime? Deadline { get; set; }
    public string? AssignedToName { get; set; }
    public DateTime? HearingDate { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class AppealDetailDto : AppealDto
{
    public Guid? TaxAssessmentId { get; set; }
    public Guid? TaxBillId { get; set; }
    public string? DetailedStatement { get; set; }
    public string? LegalBasis { get; set; }
    public string? DecisionBy { get; set; }
    public DateTime? DecisionAt { get; set; }
    public string? DecisionNotes { get; set; }
    public decimal? RevisedAssessmentValue { get; set; }
    public string? HearingNotes { get; set; }
    public List<AppealDocumentDto> Documents { get; set; } = [];
}

public class SubmitAppealRequest
{
    public Guid PropertyId { get; set; }
    public Guid TaxpayerId { get; set; }
    public Guid? TaxAssessmentId { get; set; }
    public Guid? TaxBillId { get; set; }
    public string GroundsSummary { get; set; } = string.Empty;
    public string? DetailedStatement { get; set; }
    public decimal? RequestedAssessmentValue { get; set; }
    public string? LegalBasis { get; set; }
}

public class AssignAppealRequest
{
    public Guid AssignedToUserId { get; set; }
    public DateTime? HearingDate { get; set; }
}

public class AppealDecisionRequest
{
    public AppealStatus Decision { get; set; }  // Approved, PartiallyApproved, Rejected
    public string DecisionNotes { get; set; } = string.Empty;
    public decimal? RevisedAssessmentValue { get; set; }
    public string? HearingNotes { get; set; }
}

public class AppealDocumentDto
{
    public Guid Id { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public string? DocumentType { get; set; }
    public string? Description { get; set; }
    public bool IsOfficial { get; set; }
    public string FileUrl { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}
