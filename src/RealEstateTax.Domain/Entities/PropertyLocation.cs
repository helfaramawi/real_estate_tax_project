using NetTopologySuite.Geometries;
using RealEstateTax.Domain.Common;

namespace RealEstateTax.Domain.Entities;

public class PropertyLocation : BaseEntity
{
    public Guid PropertyId { get; set; }
    public Property Property { get; set; } = null!;

    // PostGIS geometry (SRID 4326 — WGS84)
    public Point? Coordinates { get; set; }     // Centroid point
    public Polygon? Boundary { get; set; }       // Property boundary polygon

    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    public double? Elevation { get; set; }

    // Administrative hierarchy
    public string? Governorate { get; set; }
    public string? Markaz { get; set; }          // District/Center
    public string? City { get; set; }
    public string? Village { get; set; }
    public string? Neighbourhood { get; set; }
    public string? Block { get; set; }
    public string? Plot { get; set; }

    // Accuracy metadata
    public string? GeocodingSource { get; set; }  // GPS, Manual, Geocoded
    public double? GeocodingAccuracy { get; set; }
    public DateTime? GeocodedAt { get; set; }

    public bool IsVerified { get; set; }
}
