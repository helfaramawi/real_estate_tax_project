using RealEstateTax.Domain.Common;
using RealEstateTax.Domain.Enums;

namespace RealEstateTax.Domain.Entities;

public class FieldSurvey : BaseEntity
{
    public string SurveyCode { get; set; } = string.Empty;
    public Guid PropertyId { get; set; }
    public Property Property { get; set; } = null!;

    public SurveyStatus Status { get; set; } = SurveyStatus.Assigned;

    public Guid AssignedToUserId { get; set; }
    public User AssignedToUser { get; set; } = null!;

    public DateTime? AssignedAt { get; set; }
    public DateTime? ScheduledDate { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime? SubmittedAt { get; set; }
    public DateTime? ReviewedAt { get; set; }

    // Observations
    public string? FieldNotes { get; set; }
    public decimal? MeasuredArea { get; set; }
    public string? ObservedCondition { get; set; }
    public bool? IsOccupied { get; set; }
    public string? OccupantInfo { get; set; }
    public bool? OwnerContactMade { get; set; }
    public string? PropertyTypeObserved { get; set; }
    public int? NumberOfFloorsObserved { get; set; }

    // GPS captured on-site
    public double? GpsLatitude { get; set; }
    public double? GpsLongitude { get; set; }
    public double? GpsAccuracy { get; set; }

    public string? ReviewNotes { get; set; }
    public string? ReviewedBy { get; set; }

    public ICollection<SurveyAttachment> Attachments { get; set; } = [];
}

public class SurveyAttachment : BaseEntity
{
    public Guid SurveyId { get; set; }
    public FieldSurvey Survey { get; set; } = null!;

    public string FileName { get; set; } = string.Empty;
    public string StoragePath { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long FileSizeBytes { get; set; }
    public string? AttachmentType { get; set; }  // Photo, Document, Signature
    public string? Description { get; set; }
    public double? GpsLatitude { get; set; }
    public double? GpsLongitude { get; set; }
    public DateTime CapturedAt { get; set; } = DateTime.UtcNow;
}
