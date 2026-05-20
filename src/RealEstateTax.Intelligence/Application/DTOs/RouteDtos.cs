namespace RealEstateTax.Intelligence.Application.DTOs;

public class OptimizeRouteDto
{
    public Guid InspectorId { get; set; }
    public DateOnly Date { get; set; }
    public List<Guid> PropertyIds { get; set; } = [];
    public double StartLat { get; set; }
    public double StartLon { get; set; }
}

public class RouteAssignmentDto
{
    public Guid Id { get; set; }
    public Guid InspectorId { get; set; }
    public DateOnly AssignmentDate { get; set; }
    public string Status { get; set; } = string.Empty;
    public List<WaypointDto> Waypoints { get; set; } = [];
    public double? EstimatedDistanceKm { get; set; }
    public int? EstimatedDurationMin { get; set; }
    public int PropertiesVisited { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class WaypointDto
{
    public Guid PropertyId { get; set; }
    public double Lat { get; set; }
    public double Lon { get; set; }
    public int Sequence { get; set; }
    public string? PropertyCode { get; set; }
    public string? Address { get; set; }
}

public class GpsTrackPointDto
{
    public double Lat { get; set; }
    public double Lon { get; set; }
    public double? AccuracyMeters { get; set; }
    public double? SpeedKmh { get; set; }
    public double? HeadingDegrees { get; set; }
    public double? AltitudeM { get; set; }
    public int? BatteryLevel { get; set; }
    public DateTime RecordedAt { get; set; }
}

public class GpsTrackUploadDto
{
    public string DeviceId { get; set; } = string.Empty;
    public Guid? FieldSurveyId { get; set; }
    public List<GpsTrackPointDto> Points { get; set; } = [];
}
