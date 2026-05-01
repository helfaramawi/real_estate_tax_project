using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RealEstateTax.Intelligence.Domain.Entities;

namespace RealEstateTax.Intelligence.Infrastructure.Persistence.Configurations;

public class FeatureVectorConfiguration : IEntityTypeConfiguration<FeatureVector>
{
    public void Configure(EntityTypeBuilder<FeatureVector> b)
    {
        b.HasKey(x => x.Id);
        b.HasIndex(x => new { x.PropertyId, x.FeatureVersion }).IsUnique();
        b.HasIndex(x => x.ComputedAt);
        b.Property(x => x.FeatureVersion).HasMaxLength(20).IsRequired();
        b.Property(x => x.DeclaredAnnualValue).HasColumnType("numeric(18,2)");
        b.Property(x => x.TotalPaidEgp).HasColumnType("numeric(18,2)");
        b.Property(x => x.TotalOutstandingEgp).HasColumnType("numeric(18,2)");
    }
}
