using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RealEstateTax.Intelligence.Domain.Entities;

namespace RealEstateTax.Intelligence.Infrastructure.Persistence.Configurations;

public class ModelRegistrationConfiguration : IEntityTypeConfiguration<ModelRegistration>
{
    public void Configure(EntityTypeBuilder<ModelRegistration> b)
    {
        b.HasKey(x => x.Id);
        b.HasIndex(x => new { x.ModelName, x.Version }).IsUnique();
        b.HasIndex(x => new { x.Status, x.PredictionType });
        b.Property(x => x.ModelName).HasMaxLength(100).IsRequired();
        b.Property(x => x.ModelType).HasMaxLength(50).IsRequired();
        b.Property(x => x.Version).HasMaxLength(20).IsRequired();
        b.Property(x => x.Status).HasConversion<string>().HasMaxLength(20);
        b.Property(x => x.PredictionType).HasConversion<string>().HasMaxLength(50);
        b.Property(x => x.Metrics).HasColumnType("jsonb");
        b.Property(x => x.FeatureImportance).HasColumnType("jsonb");

        b.HasMany(x => x.Predictions)
         .WithOne(x => x.Model)
         .HasForeignKey(x => x.ModelId)
         .OnDelete(DeleteBehavior.Restrict);
    }
}
