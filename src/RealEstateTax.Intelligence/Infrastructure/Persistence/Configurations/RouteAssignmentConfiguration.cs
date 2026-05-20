using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RealEstateTax.Intelligence.Domain.Entities;

namespace RealEstateTax.Intelligence.Infrastructure.Persistence.Configurations;

public class RouteAssignmentConfiguration : IEntityTypeConfiguration<RouteAssignment>
{
    public void Configure(EntityTypeBuilder<RouteAssignment> b)
    {
        b.HasKey(x => x.Id);
        b.Property(x => x.OptimizedRoute).HasColumnType("geometry(LineString, 4326)");
        b.HasIndex(x => x.OptimizedRoute).HasMethod("GIST");
        b.HasIndex(x => new { x.InspectorId, x.AssignmentDate });
        b.Property(x => x.Status).HasMaxLength(20).IsRequired();
        b.Property(x => x.Waypoints).HasColumnType("jsonb").IsRequired();
    }
}
