using RealEstateTax.Domain.Common;
using RealEstateTax.Domain.Enums;

namespace RealEstateTax.Domain.Entities;

public class PropertySource : BaseEntity
{
    public string Code { get; set; } = string.Empty;       // e.g. "ELEC", "WATER"
    public string Name { get; set; } = string.Empty;
    public SourceSystem SourceSystem { get; set; }
    public string? Description { get; set; }
    public string? ApiEndpoint { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime? LastSyncAt { get; set; }
    public int TotalRecordsImported { get; set; }

    public ICollection<PropertySourceRecord> Records { get; set; } = [];
}

public class PropertySourceRecord : BaseEntity
{
    public Guid SourceId { get; set; }
    public PropertySource Source { get; set; } = null!;

    public Guid? MasterPropertyId { get; set; }
    public Property? MasterProperty { get; set; }

    // Raw data from source
    public string SourceReferenceId { get; set; } = string.Empty;
    public string? OwnerName { get; set; }
    public string? OwnerNationalId { get; set; }
    public string? Address { get; set; }
    public string? MeterNumber { get; set; }           // Electricity/Water meter
    public string? AccountNumber { get; set; }
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    public decimal? AreaFromSource { get; set; }
    public string? PropertyTypeFromSource { get; set; }
    public string? RawJsonPayload { get; set; }

    // Matching results
    public bool IsMatched { get; set; }
    public DateTime? MatchedAt { get; set; }
    public double? MatchConfidenceScore { get; set; }
    public string? MatchMethod { get; set; }
    public bool IsRejectedForMatch { get; set; }
    public string? RejectionReason { get; set; }

    public DateTime ImportedAt { get; set; } = DateTime.UtcNow;
    public string? ImportBatchId { get; set; }
}
