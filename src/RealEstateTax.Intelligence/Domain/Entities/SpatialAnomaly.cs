using NetTopologySuite.Geometries;
using RealEstateTax.Intelligence.Domain.Enums;

namespace RealEstateTax.Intelligence.Domain.Entities;

public class SpatialAnomaly
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public AnomalyType AnomalyType { get; set; }
    public string Severity { get; set; } = "Medium";
    public string Status { get; set; } = "Open";
    public Guid? PropertyId { get; set; }
    public Point? Location { get; set; }
    public Polygon? AffectedArea { get; set; }
    public DateTime DetectedAt { get; set; } = DateTime.UtcNow;
    public string? DetectionModel { get; set; }
    public string? Description { get; set; }
    public string? Evidence { get; set; }
    public Guid? AssignedTo { get; set; }
    public DateTime? ResolvedAt { get; set; }
    public string? ResolutionNotes { get; set; }
}
