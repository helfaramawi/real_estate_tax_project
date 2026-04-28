using RealEstateTax.Domain.Enums;

namespace RealEstateTax.Application.DTOs.Enumeration;

public class ImportSourceRecordsRequest
{
    public Guid SourceId { get; set; }
    public List<SourceRecordImportItem> Records { get; set; } = [];
    public string? BatchId { get; set; }
}

public class SourceRecordImportItem
{
    public string SourceReferenceId { get; set; } = string.Empty;
    public string? OwnerName { get; set; }
    public string? OwnerNationalId { get; set; }
    public string? Address { get; set; }
    public string? MeterNumber { get; set; }
    public string? AccountNumber { get; set; }
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    public decimal? Area { get; set; }
    public string? PropertyType { get; set; }
    public string? RawJson { get; set; }
}

public class ImportResultDto
{
    public int TotalImported { get; set; }
    public int AlreadyExisted { get; set; }
    public int Failed { get; set; }
    public string BatchId { get; set; } = string.Empty;
    public List<string> Errors { get; set; } = [];
}

public class MatchSourceRecordsRequest
{
    public List<Guid> SourceRecordIds { get; set; } = [];
    public bool CreateNewIfNoMatch { get; set; } = false;
    public double MinConfidenceThreshold { get; set; } = 0.75;
}

public class MatchResultDto
{
    public Guid SourceRecordId { get; set; }
    public bool IsMatched { get; set; }
    public Guid? MasterPropertyId { get; set; }
    public string? MasterPropertyCode { get; set; }
    public double ConfidenceScore { get; set; }
    public string? MatchMethod { get; set; }
    public string[] ContributingFactors { get; set; } = [];
}

public class CreateMasterRecordRequest
{
    public List<Guid> SourceRecordIds { get; set; } = [];
    public string? OverridePropertyType { get; set; }
    public string? OverrideAddress { get; set; }
    public bool AutoLinkOwner { get; set; } = true;
}

public class UnmatchedRecordDto
{
    public Guid Id { get; set; }
    public string SourceReferenceId { get; set; } = string.Empty;
    public string SourceCode { get; set; } = string.Empty;
    public string? OwnerName { get; set; }
    public string? OwnerNationalId { get; set; }
    public string? Address { get; set; }
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    public DateTime ImportedAt { get; set; }
}

public class DataQualityIssueDto
{
    public Guid Id { get; set; }
    public Guid PropertyId { get; set; }
    public string PropertyCode { get; set; } = string.Empty;
    public string IssueType { get; set; } = string.Empty;
    public string Severity { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string? AffectedField { get; set; }
    public string? SuggestedAction { get; set; }
    public DateTime CreatedAt { get; set; }
}
