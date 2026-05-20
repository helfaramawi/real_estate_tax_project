using NetTopologySuite.Geometries;

namespace RealEstateTax.Intelligence.Domain.Entities;

public class RouteAssignment
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid InspectorId { get; set; }
    public DateOnly AssignmentDate { get; set; }
    public string Status { get; set; } = "Planned";
    public string Waypoints { get; set; } = "[]";
    public LineString? OptimizedRoute { get; set; }
    public double? EstimatedDistanceKm { get; set; }
    public int? EstimatedDurationMin { get; set; }
    public DateTime? ActualStart { get; set; }
    public DateTime? ActualEnd { get; set; }
    public int PropertiesVisited { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
