using RealEstateTax.Domain.Common;

namespace RealEstateTax.Domain.Entities;

public class PropertyUnit : BaseEntity
{
    public Guid PropertyId { get; set; }
    public Property Property { get; set; } = null!;

    public string UnitCode { get; set; } = string.Empty;
    public string? UnitNumber { get; set; }
    public int FloorNumber { get; set; }
    public decimal Area { get; set; }       // m²
    public string? UnitType { get; set; }   // Apartment, Office, Shop, etc.
    public string? UsageType { get; set; }
    public bool IsOccupied { get; set; }
    public decimal? MonthlyRent { get; set; }  // EGP — used for rental value method
    public bool IsRented { get; set; }

    public ICollection<PropertyOwnership> Ownerships { get; set; } = [];
}
