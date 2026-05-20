using NetTopologySuite.Geometries;

namespace RealEstateTax.Intelligence.Domain.Entities;

public class GeoCluster
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string ClusterAlgorithm { get; set; } = "DBSCAN";
    public int ClusterLabel { get; set; }
    public Point? Centroid { get; set; }
    public Polygon? ConvexHull { get; set; }
    public int PropertyCount { get; set; }
    public decimal? MedianValueSqm { get; set; }
    public decimal? P25ValueSqm { get; set; }
    public decimal? P75ValueSqm { get; set; }
    public DateTime ComputedAt { get; set; } = DateTime.UtcNow;
    public string? DistrictCode { get; set; }
    public string? Governorate { get; set; }
}
