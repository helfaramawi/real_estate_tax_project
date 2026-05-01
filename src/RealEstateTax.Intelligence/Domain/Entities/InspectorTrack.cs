using NetTopologySuite.Geometries;

namespace RealEstateTax.Intelligence.Domain.Entities;

public class InspectorTrack
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid InspectorId { get; set; }
    public Guid? FieldSurveyId { get; set; }
    public DateTime RecordedAt { get; set; }
    public DateTime SyncedAt { get; set; } = DateTime.UtcNow;
    public Point? Location { get; set; }
    public double? AccuracyMeters { get; set; }
    public double? SpeedKmh { get; set; }
    public double? HeadingDegrees { get; set; }
    public double? AltitudeM { get; set; }
    public int? BatteryLevel { get; set; }
    public string? DeviceId { get; set; }
    public bool IsOfflineRecord { get; set; }
}
