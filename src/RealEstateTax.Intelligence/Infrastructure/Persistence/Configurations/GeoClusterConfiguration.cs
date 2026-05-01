using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RealEstateTax.Intelligence.Domain.Entities;

namespace RealEstateTax.Intelligence.Infrastructure.Persistence.Configurations;

public class GeoClusterConfiguration : IEntityTypeConfiguration<GeoCluster>
{
    public void Configure(EntityTypeBuilder<GeoCluster> b)
    {
        b.HasKey(x => x.Id);
        b.Property(x => x.Centroid).HasColumnType("geometry(Point, 4326)");
        b.Property(x => x.ConvexHull).HasColumnType("geometry(Polygon, 4326)");
        b.HasIndex(x => x.Centroid).HasMethod("GIST");
        b.HasIndex(x => x.ConvexHull).HasMethod("GIST");
        b.Property(x => x.MedianValueSqm).HasColumnType("numeric(18,2)");
        b.Property(x => x.P25ValueSqm).HasColumnType("numeric(18,2)");
        b.Property(x => x.P75ValueSqm).HasColumnType("numeric(18,2)");
        b.Property(x => x.ClusterAlgorithm).HasMaxLength(30);
        b.Property(x => x.DistrictCode).HasMaxLength(50);
        b.Property(x => x.Governorate).HasMaxLength(100);
    }
}
