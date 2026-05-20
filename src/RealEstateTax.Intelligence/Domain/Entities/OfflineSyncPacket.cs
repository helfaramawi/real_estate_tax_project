using RealEstateTax.Intelligence.Domain.Enums;

namespace RealEstateTax.Intelligence.Domain.Entities;

public class OfflineSyncPacket
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string DeviceId { get; set; } = string.Empty;
    public Guid InspectorId { get; set; }
    public DateTime SubmittedAt { get; set; } = DateTime.UtcNow;
    public SyncPacketType PacketType { get; set; }
    public string Payload { get; set; } = "{}";
    public string Status { get; set; } = "Pending";
    public DateTime? ProcessedAt { get; set; }
    public string? ErrorMessage { get; set; }
    public int RetryCount { get; set; }
}
