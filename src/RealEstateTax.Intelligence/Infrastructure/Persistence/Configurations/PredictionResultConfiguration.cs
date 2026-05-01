using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RealEstateTax.Intelligence.Domain.Entities;

namespace RealEstateTax.Intelligence.Infrastructure.Persistence.Configurations;

public class PredictionResultConfiguration : IEntityTypeConfiguration<PredictionResult>
{
    public void Configure(EntityTypeBuilder<PredictionResult> b)
    {
        b.HasKey(x => x.Id);
        b.HasIndex(x => new { x.PropertyId, x.PredictionType });
        b.HasIndex(x => new { x.PredictionType, x.PredictedAt });
        b.HasIndex(x => x.Score).HasFilter("is_reviewed = false");
        b.Property(x => x.PredictionType).HasConversion<string>().HasMaxLength(50);
        b.Property(x => x.Label).HasMaxLength(50);
        b.Property(x => x.ReviewOutcome).HasMaxLength(20);
        b.Property(x => x.Explanation).HasColumnType("jsonb");
        b.Property(x => x.FeatureSnapshot).HasColumnType("jsonb");
    }
}
