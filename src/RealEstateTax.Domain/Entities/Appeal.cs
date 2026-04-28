using RealEstateTax.Domain.Common;
using RealEstateTax.Domain.Enums;

namespace RealEstateTax.Domain.Entities;

public class Appeal : BaseEntity
{
    public string AppealCode { get; set; } = string.Empty;
    public Guid PropertyId { get; set; }
    public Property Property { get; set; } = null!;

    public Guid TaxpayerId { get; set; }
    public Taxpayer Taxpayer { get; set; } = null!;

    public Guid? TaxAssessmentId { get; set; }
    public TaxAssessment? TaxAssessment { get; set; }

    public Guid? TaxBillId { get; set; }
    public TaxBill? TaxBill { get; set; }

    public AppealStatus Status { get; set; } = AppealStatus.Submitted;

    public string GroundsSummary { get; set; } = string.Empty;
    public string? DetailedStatement { get; set; }
    public decimal? RequestedAssessmentValue { get; set; }   // EGP — what taxpayer claims
    public string? LegalBasis { get; set; }

    public DateTime SubmittedAt { get; set; } = DateTime.UtcNow;
    public DateTime? Deadline { get; set; }
    // TODO: Set appeal deadline per Egyptian law (typically 60 days from assessment notice)

    // Assignment (Maker-Checker workflow)
    public Guid? AssignedToUserId { get; set; }
    public User? AssignedToUser { get; set; }
    public DateTime? AssignedAt { get; set; }

    // Hearing
    public DateTime? HearingDate { get; set; }
    public string? HearingNotes { get; set; }

    // Decision
    public string? DecisionBy { get; set; }
    public DateTime? DecisionAt { get; set; }
    public string? DecisionNotes { get; set; }
    public decimal? RevisedAssessmentValue { get; set; }     // EGP — if partially approved

    public ICollection<AppealDocument> Documents { get; set; } = [];
}

public class AppealDocument : BaseEntity
{
    public Guid AppealId { get; set; }
    public Appeal Appeal { get; set; } = null!;

    public string FileName { get; set; } = string.Empty;
    public string StoragePath { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long FileSizeBytes { get; set; }
    public string? DocumentType { get; set; }   // SupportingEvidence, LegalDocument, etc.
    public string? Description { get; set; }
    public bool IsOfficial { get; set; }        // Uploaded by authority vs. taxpayer
}
