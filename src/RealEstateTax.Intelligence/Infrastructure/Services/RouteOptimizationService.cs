using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using RealEstateTax.Intelligence.Application.DTOs;
using RealEstateTax.Intelligence.Application.Interfaces;
using RealEstateTax.Intelligence.Domain.Entities;
using RealEstateTax.Intelligence.Infrastructure.Persistence;

namespace RealEstateTax.Intelligence.Infrastructure.Services;

public class RouteOptimizationService(
    IntelligenceDbContext intelDb,
    RealEstateTax.Infrastructure.Persistence.ApplicationDbContext appDb,
    ILogger<RouteOptimizationService> logger) : IRouteOptimizationService
{
    public async Task<RouteAssignment> OptimizeRouteAsync(OptimizeRouteDto request, CancellationToken ct = default)
    {
        // Fetch property locations for the requested property IDs
        var locations = await appDb.PropertyLocations
            .Where(pl => request.PropertyIds.Contains(pl.PropertyId))
            .Select(pl => new { pl.PropertyId, pl.Latitude, pl.Longitude })
            .AsNoTracking()
            .ToListAsync(ct);

        // Nearest-neighbor heuristic for TSP (OSRM integration is optional)
        // Flatten nullable coords to non-nullable, skipping entries without coordinates
        var stops = locations
            .Where(l => l.Latitude.HasValue && l.Longitude.HasValue)
            .Select(l => (l.PropertyId, Latitude: l.Latitude!.Value, Longitude: l.Longitude!.Value))
            .ToList();

        var ordered = NearestNeighborTsp(request.StartLat, request.StartLon, stops);

        var waypoints = ordered.Select((l, idx) => new WaypointDto
        {
            PropertyId = l.PropertyId,
            Lat = l.Latitude,
            Lon = l.Longitude,
            Sequence = idx + 1,
        }).ToList();

        var totalDistanceKm = EstimateDistance(
            (request.StartLat, request.StartLon),
            waypoints.Select(w => (w.Lat, w.Lon)).ToList());

        var assignment = new RouteAssignment
        {
            InspectorId = request.InspectorId,
            AssignmentDate = request.Date,
            Status = "Planned",
            Waypoints = System.Text.Json.JsonSerializer.Serialize(waypoints),
            EstimatedDistanceKm = totalDistanceKm,
            EstimatedDurationMin = (int)(totalDistanceKm / 30.0 * 60), // assume 30 km/h avg
        };

        intelDb.RouteAssignments.Add(assignment);
        await intelDb.SaveChangesAsync(ct);
        logger.LogInformation("Route optimized for inspector {Id}: {Count} waypoints, {Dist:F1} km",
            request.InspectorId, waypoints.Count, totalDistanceKm);
        return assignment;
    }

    public Task<List<RouteAssignment>> GetAssignmentsAsync(
        Guid inspectorId, DateOnly date, CancellationToken ct = default)
        => intelDb.RouteAssignments
            .Where(r => r.InspectorId == inspectorId && r.AssignmentDate == date)
            .AsNoTracking()
            .ToListAsync(ct);

    public async Task<RouteAssignment?> UpdateStatusAsync(Guid id, string status, CancellationToken ct = default)
    {
        var r = await intelDb.RouteAssignments.FindAsync([id], ct);
        if (r is null) return null;
        r.Status = status;
        if (status == "Active") r.ActualStart ??= DateTime.UtcNow;
        if (status == "Completed") r.ActualEnd = DateTime.UtcNow;
        await intelDb.SaveChangesAsync(ct);
        return r;
    }

    public Task<List<InspectorTrack>> GetTrackAsync(Guid assignmentId, CancellationToken ct = default)
        => intelDb.InspectorTracks
            .Where(t => t.FieldSurveyId != null)
            .OrderBy(t => t.RecordedAt)
            .AsNoTracking()
            .ToListAsync(ct);

    private static List<(Guid PropertyId, double Latitude, double Longitude)> NearestNeighborTsp(
        double startLat, double startLon,
        List<(Guid PropertyId, double Latitude, double Longitude)> stops)
    {
        var unvisited = new List<(Guid PropertyId, double Latitude, double Longitude)>(stops);
        var result = new List<(Guid PropertyId, double Latitude, double Longitude)>();
        var curLat = startLat;
        var curLon = startLon;

        while (unvisited.Count > 0)
        {
            var nearest = unvisited.MinBy(s => HaversineKm(curLat, curLon, s.Latitude, s.Longitude));
            result.Add(nearest);
            unvisited.Remove(nearest);
            curLat = nearest.Latitude;
            curLon = nearest.Longitude;
        }
        return result;
    }

    private static double EstimateDistance(
        (double Lat, double Lon) start,
        List<(double Lat, double Lon)> stops)
    {
        if (stops.Count == 0) return 0;
        var total = HaversineKm(start.Lat, start.Lon, stops[0].Lat, stops[0].Lon);
        for (int i = 0; i < stops.Count - 1; i++)
            total += HaversineKm(stops[i].Lat, stops[i].Lon, stops[i + 1].Lat, stops[i + 1].Lon);
        return total;
    }

    private static double HaversineKm(double lat1, double lon1, double lat2, double lon2)
    {
        const double R = 6371.0;
        var dLat = (lat2 - lat1) * Math.PI / 180;
        var dLon = (lon2 - lon1) * Math.PI / 180;
        var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                Math.Cos(lat1 * Math.PI / 180) * Math.Cos(lat2 * Math.PI / 180) *
                Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
        return R * 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
    }
}
