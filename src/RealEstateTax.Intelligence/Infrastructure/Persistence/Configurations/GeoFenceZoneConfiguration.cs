using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RealEstateTax.Intelligence.Domain.Entities;

namespace RealEstateTax.Intelligence.Infrastructure.Persistence.Configurations;

public class GeoFenceZoneConfiguration : IEntityTypeConfiguration<GeoFenceZone>
{
    public void Configure(EntityTypeBuilder<GeoFenceZone> b)
    {
        b.HasKey(x => x.Id);
        b.Property(x => x.Boundary).HasColumnType("geometry(Polygon, 4326)").IsRequired();
        b.HasIndex(x => x.Boundary).HasMethod("GIST");
        b.HasIndex(x => new { x.IsActive, x.ZoneType });
        b.Property(x => x.Name).HasMaxLength(200).IsRequired();
        b.Property(x => x.ZoneType).HasMaxLength(50).IsRequired();
        b.Property(x => x.Properties).HasColumnType("jsonb");
    }
}
