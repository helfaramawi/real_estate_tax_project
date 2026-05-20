using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RealEstateTax.Intelligence.Domain.Entities;

namespace RealEstateTax.Intelligence.Infrastructure.Persistence.Configurations;

public class SpatialAnomalyConfiguration : IEntityTypeConfiguration<SpatialAnomaly>
{
    public void Configure(EntityTypeBuilder<SpatialAnomaly> b)
    {
        b.HasKey(x => x.Id);
        b.Property(x => x.Location).HasColumnType("geometry(Point, 4326)");
        b.Property(x => x.AffectedArea).HasColumnType("geometry(Polygon, 4326)");
        b.HasIndex(x => x.Location).HasMethod("GIST");
        b.HasIndex(x => new { x.Status, x.Severity });
        b.HasIndex(x => x.PropertyId);
        b.Property(x => x.AnomalyType).HasConversion<string>().HasMaxLength(50);
        b.Property(x => x.Severity).HasMaxLength(20).IsRequired();
        b.Property(x => x.Status).HasMaxLength(20).IsRequired();
        b.Property(x => x.DetectionModel).HasMaxLength(100);
        b.Property(x => x.Evidence).HasColumnType("jsonb");
    }
}
