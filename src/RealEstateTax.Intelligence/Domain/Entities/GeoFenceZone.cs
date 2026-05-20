using NetTopologySuite.Geometries;

namespace RealEstateTax.Intelligence.Domain.Entities;

public class GeoFenceZone
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public string ZoneType { get; set; } = string.Empty;
    public Polygon Boundary { get; set; } = null!;
    public string? Properties { get; set; }
    public bool IsActive { get; set; } = true;
    public DateOnly? ValidFrom { get; set; }
    public DateOnly? ValidTo { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
