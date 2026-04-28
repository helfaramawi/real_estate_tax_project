using RealEstateTax.Domain.Enums;

namespace RealEstateTax.Application.DTOs.FieldSurveys;

public class FieldSurveyDto
{
    public Guid Id { get; set; }
    public string SurveyCode { get; set; } = string.Empty;
    public Guid PropertyId { get; set; }
    public string PropertyCode { get; set; } = string.Empty;
    public string? PropertyAddress { get; set; }
    public SurveyStatus Status { get; set; }
    public Guid AssignedToUserId { get; set; }
    public string AssignedToName { get; set; } = string.Empty;
    public DateTime? ScheduledDate { get; set; }
    public DateTime? SubmittedAt { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class FieldSurveyDetailDto : FieldSurveyDto
{
    public string? FieldNotes { get; set; }
    public decimal? MeasuredArea { get; set; }
    public string? ObservedCondition { get; set; }
    public bool? IsOccupied { get; set; }
    public string? OccupantInfo { get; set; }
    public bool? OwnerContactMade { get; set; }
    public string? PropertyTypeObserved { get; set; }
    public int? NumberOfFloorsObserved { get; set; }
    public double? GpsLatitude { get; set; }
    public double? GpsLongitude { get; set; }
    public string? ReviewNotes { get; set; }
    public string? ReviewedBy { get; set; }
    public List<SurveyAttachmentDto> Attachments { get; set; } = [];
}

public class CreateFieldSurveyRequest
{
    public Guid PropertyId { get; set; }
    public Guid AssignedToUserId { get; set; }
    public DateTime? ScheduledDate { get; set; }
}

public class UpdateFieldSurveyRequest
{
    public string? FieldNotes { get; set; }
    public decimal? MeasuredArea { get; set; }
    public string? ObservedCondition { get; set; }
    public bool? IsOccupied { get; set; }
    public string? OccupantInfo { get; set; }
    public bool? OwnerContactMade { get; set; }
    public string? PropertyTypeObserved { get; set; }
    public int? NumberOfFloorsObserved { get; set; }
    public double? GpsLatitude { get; set; }
    public double? GpsLongitude { get; set; }
    public double? GpsAccuracy { get; set; }
}

public class SurveyAttachmentDto
{
    public Guid Id { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long FileSizeBytes { get; set; }
    public string? AttachmentType { get; set; }
    public string? Description { get; set; }
    public double? GpsLatitude { get; set; }
    public double? GpsLongitude { get; set; }
    public DateTime CapturedAt { get; set; }
    public string FileUrl { get; set; } = string.Empty;
}
