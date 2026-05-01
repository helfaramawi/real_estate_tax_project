using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RealEstateTax.Intelligence.Domain.Entities;

namespace RealEstateTax.Intelligence.Infrastructure.Persistence.Configurations;

public class InspectorTrackConfiguration : IEntityTypeConfiguration<InspectorTrack>
{
    public void Configure(EntityTypeBuilder<InspectorTrack> b)
    {
        b.HasKey(x => x.Id);
        b.Property(x => x.Location).HasColumnType("geometry(Point, 4326)");
        b.HasIndex(x => x.Location).HasMethod("GIST");
        b.HasIndex(x => new { x.InspectorId, x.RecordedAt });
        b.HasIndex(x => x.FieldSurveyId);
        b.Property(x => x.DeviceId).HasMaxLength(100);
    }
}
