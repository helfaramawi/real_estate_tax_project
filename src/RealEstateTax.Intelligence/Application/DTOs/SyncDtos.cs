namespace RealEstateTax.Intelligence.Application.DTOs;

public class UploadSyncPacketsDto
{
    public string DeviceId { get; set; } = string.Empty;
    public List<SyncPacketDto> Packets { get; set; } = [];
}

public class SyncPacketDto
{
    public string Type { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public object Payload { get; set; } = new();
}

public class SyncUploadResultDto
{
    public Guid SyncId { get; set; }
    public int PacketsReceived { get; set; }
    public string Status { get; set; } = "Queued";
}
