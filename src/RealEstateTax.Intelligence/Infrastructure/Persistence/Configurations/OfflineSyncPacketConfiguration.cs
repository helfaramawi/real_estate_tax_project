using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RealEstateTax.Intelligence.Domain.Entities;

namespace RealEstateTax.Intelligence.Infrastructure.Persistence.Configurations;

public class OfflineSyncPacketConfiguration : IEntityTypeConfiguration<OfflineSyncPacket>
{
    public void Configure(EntityTypeBuilder<OfflineSyncPacket> b)
    {
        b.HasKey(x => x.Id);
        b.HasIndex(x => new { x.Status, x.SubmittedAt });
        b.HasIndex(x => new { x.InspectorId, x.SubmittedAt });
        b.Property(x => x.DeviceId).HasMaxLength(100).IsRequired();
        b.Property(x => x.PacketType).HasConversion<string>().HasMaxLength(50);
        b.Property(x => x.Status).HasMaxLength(20).IsRequired();
        b.Property(x => x.Payload).HasColumnType("jsonb").IsRequired();
    }
}
