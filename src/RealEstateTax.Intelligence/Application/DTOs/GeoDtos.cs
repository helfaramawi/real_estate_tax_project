namespace RealEstateTax.Intelligence.Application.DTOs;

public class GeoClusterDto
{
    public Guid Id { get; set; }
    public int ClusterLabel { get; set; }
    public double? CentroidLat { get; set; }
    public double? CentroidLon { get; set; }
    public string? ConvexHullGeoJson { get; set; }
    public int PropertyCount { get; set; }
    public decimal? MedianValueSqm { get; set; }
    public decimal? P25ValueSqm { get; set; }
    public decimal? P75ValueSqm { get; set; }
    public string? Governorate { get; set; }
    public DateTime ComputedAt { get; set; }
}

public class SpatialAnomalyDto
{
    public Guid Id { get; set; }
    public string AnomalyType { get; set; } = string.Empty;
    public string Severity { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public Guid? PropertyId { get; set; }
    public double? Lat { get; set; }
    public double? Lon { get; set; }
    public string? Description { get; set; }
    public DateTime DetectedAt { get; set; }
}

public record UpdateAnomalyStatusDto(string Status, string? Notes);

public class CreateGeoFenceZoneDto
{
    public string Name { get; set; } = string.Empty;
    public string ZoneType { get; set; } = string.Empty;
    public string GeoJson { get; set; } = string.Empty;
    public Dictionary<string, object>? Properties { get; set; }
    public DateOnly? ValidFrom { get; set; }
    public DateOnly? ValidTo { get; set; }
}

public class GeoFenceZoneDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string ZoneType { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public string? BoundaryGeoJson { get; set; }
}

public class RiskHeatmapDto
{
    public List<HeatmapCellDto> Cells { get; set; } = [];
}

public class HeatmapCellDto
{
    public double CenterLat { get; set; }
    public double CenterLon { get; set; }
    public double AvgRiskScore { get; set; }
    public int PropertyCount { get; set; }
    public string H3Index { get; set; } = string.Empty;
}
