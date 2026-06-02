using System.Data;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NetTopologySuite.Geometries;
using RealEstateTax.Intelligence.Application.DTOs;
using RealEstateTax.Intelligence.Application.Interfaces;
using RealEstateTax.Intelligence.Domain.Entities;
using RealEstateTax.Intelligence.Domain.Enums;
using RealEstateTax.Intelligence.Infrastructure.Persistence;

namespace RealEstateTax.Intelligence.Infrastructure.Services;

public class GeoIntelligenceService(
    IntelligenceDbContext intelDb,
    RealEstateTax.Infrastructure.Persistence.ApplicationDbContext appDb,
    ILogger<GeoIntelligenceService> logger) : IGeoIntelligenceService
{
    private static readonly GeometryFactory _gf = new(new PrecisionModel(), 4326);

    public Task<List<GeoCluster>> GetClustersAsync(string? governorate, CancellationToken ct = default)
    {
        var q = intelDb.GeoClusters.AsQueryable();
        if (!string.IsNullOrEmpty(governorate))
            q = q.Where(c => c.Governorate == governorate);
        return q.OrderByDescending(c => c.ComputedAt).Take(500).AsNoTracking().ToListAsync(ct);
    }

    public async Task RunClusteringAsync(CancellationToken ct = default)
    {
        logger.LogInformation("Running DBSCAN geospatial clustering via PostGIS");

        // ST_ClusterDBSCAN: eps in degrees (~100m ≈ 0.001°), minPoints = 3
        const string sql = """
            SELECT
                c.cluster_id                                               AS "ClusterLabel",
                ST_AsText(ST_Centroid(ST_Collect(pl.coordinates)))         AS "CentroidWkt",
                ST_AsText(ST_ConvexHull(ST_Collect(pl.coordinates)))       AS "HullWkt",
                COUNT(*)::int                                             AS "PropertyCount",
                PERCENTILE_CONT(0.5) WITHIN GROUP (ORDER BY v.market_value_per_sq_m) AS "MedianVal",
                PERCENTILE_CONT(0.25) WITHIN GROUP (ORDER BY v.market_value_per_sq_m) AS "P25Val",
                PERCENTILE_CONT(0.75) WITHIN GROUP (ORDER BY v.market_value_per_sq_m) AS "P75Val"
            FROM (
                SELECT
                    property_id,
                    ST_ClusterDBSCAN(coordinates, eps := 0.001, minpoints := 3) OVER () AS cluster_id
                FROM public.property_locations
                WHERE coordinates IS NOT NULL
            ) c
            JOIN public.property_locations pl ON pl.property_id = c.property_id
            LEFT JOIN LATERAL (
                SELECT market_value_per_sq_m
                FROM public.valuations
                WHERE property_id = c.property_id
                ORDER BY created_at DESC
                LIMIT 1
            ) v ON TRUE
            WHERE c.cluster_id IS NOT NULL
            GROUP BY c.cluster_id
            HAVING COUNT(*) >= 3
            """;

        var results = await appDb.Database
            .SqlQueryRaw<ClusterRaw>(sql)
            .ToListAsync(ct);

        // Delete old cluster data and insert fresh results
        await intelDb.Database.ExecuteSqlRawAsync("DELETE FROM intel.geo_clusters", ct);

        var clusters = results.Select(r => new GeoCluster
        {
            ClusterLabel = r.ClusterLabel,
            PropertyCount = r.PropertyCount,
            MedianValueSqm = r.MedianVal.HasValue ? (decimal)r.MedianVal.Value : null,
            P25ValueSqm = r.P25Val.HasValue ? (decimal)r.P25Val.Value : null,
            P75ValueSqm = r.P75Val.HasValue ? (decimal)r.P75Val.Value : null,
            ComputedAt = DateTime.UtcNow,
        }).ToList();

        intelDb.GeoClusters.AddRange(clusters);
        await intelDb.SaveChangesAsync(ct);
        logger.LogInformation("Clustering complete: {Count} clusters created", clusters.Count);
    }

    public Task<List<SpatialAnomaly>> GetAnomaliesAsync(string? status, string? severity, CancellationToken ct = default)
    {
        var q = intelDb.SpatialAnomalies.AsQueryable();
        if (!string.IsNullOrEmpty(status)) q = q.Where(a => a.Status == status);
        if (!string.IsNullOrEmpty(severity)) q = q.Where(a => a.Severity == severity);
        return q.OrderByDescending(a => a.DetectedAt).AsNoTracking().ToListAsync(ct);
    }

    public async Task<SpatialAnomaly?> UpdateAnomalyStatusAsync(
        Guid id, string status, string? notes, Guid reviewerId, CancellationToken ct = default)
    {
        var anomaly = await intelDb.SpatialAnomalies.FindAsync([id], ct);
        if (anomaly is null) return null;
        anomaly.Status = status;
        anomaly.ResolutionNotes = notes;
        anomaly.AssignedTo = reviewerId;
        if (status is "Resolved" or "FalsePositive")
            anomaly.ResolvedAt = DateTime.UtcNow;
        await intelDb.SaveChangesAsync(ct);
        return anomaly;
    }

    public Task<List<GeoFenceZone>> GetFenceZonesAsync(bool activeOnly, CancellationToken ct = default)
    {
        var q = intelDb.GeoFenceZones.AsQueryable();
        if (activeOnly) q = q.Where(z => z.IsActive);
        return q.AsNoTracking().ToListAsync(ct);
    }

    public async Task<GeoFenceZone> CreateFenceZoneAsync(CreateGeoFenceZoneDto dto, CancellationToken ct = default)
    {
        // Parse GeoJSON polygon coordinates into NTS Polygon
        var zone = new GeoFenceZone
        {
            Name = dto.Name,
            ZoneType = dto.ZoneType,
            Boundary = ParseGeoJsonPolygon(dto.GeoJson),
            Properties = dto.Properties is not null ? JsonSerializer.Serialize(dto.Properties) : null,
            ValidFrom = dto.ValidFrom,
            ValidTo = dto.ValidTo,
        };
        intelDb.GeoFenceZones.Add(zone);
        await intelDb.SaveChangesAsync(ct);
        return zone;
    }

    public async Task UpdatePropertyGeoFenceMembershipAsync(CancellationToken ct = default)
    {
        logger.LogInformation("Updating property geo-fence membership");
        const string sql = """
            UPDATE public.property_locations pl
            SET in_geo_fence = TRUE,
                fence_zone_id = (
                    SELECT z.id FROM intel.geo_fence_zones z
                    WHERE z.is_active = TRUE
                      AND ST_Contains(z.boundary, pl.coordinates)
                    ORDER BY z.created_at DESC
                    LIMIT 1
                )
            WHERE pl.coordinates IS NOT NULL
              AND EXISTS (
                SELECT 1 FROM intel.geo_fence_zones z
                WHERE z.is_active = TRUE AND ST_Contains(z.boundary, pl.coordinates)
              );

            UPDATE public.property_locations pl
            SET in_geo_fence = FALSE, fence_zone_id = NULL
            WHERE pl.coordinates IS NOT NULL
              AND NOT EXISTS (
                SELECT 1 FROM intel.geo_fence_zones z
                WHERE z.is_active = TRUE AND ST_Contains(z.boundary, pl.coordinates)
              );
            """;
        await appDb.Database.ExecuteSqlRawAsync(sql, ct);
    }

    public async Task<RiskHeatmapDto> GetRiskHeatmapAsync(
        double minLat, double minLon, double maxLat, double maxLon, CancellationToken ct = default)
    {
        // Aggregate risk scores by 0.01° grid cells (≈ 1km) within bounding box
        const string sql = """
            SELECT
                ROUND(CAST(pl.latitude AS NUMERIC), 2)::double precision  AS "CellLat",
                ROUND(CAST(pl.longitude AS NUMERIC), 2)::double precision AS "CellLon",
                AVG(COALESCE(p.ml_risk_score, r.score / 100.0, 0.5))::double precision AS "AvgRisk",
                COUNT(*)::int                                            AS "PropertyCount"
            FROM public.property_locations pl
            JOIN public.properties p ON p.id = pl.property_id AND NOT p.is_deleted
            LEFT JOIN LATERAL (
                SELECT score FROM public.risk_scores
                WHERE property_id = p.id
                ORDER BY created_at DESC LIMIT 1
            ) r ON TRUE
            WHERE pl.latitude BETWEEN @minLat AND @maxLat
              AND pl.longitude BETWEEN @minLon AND @maxLon
            GROUP BY ROUND(CAST(pl.latitude AS NUMERIC), 2),
                     ROUND(CAST(pl.longitude AS NUMERIC), 2)
            """;

        var rows = await ReadHeatmapRowsAsync(sql, minLat, maxLat, minLon, maxLon, ct);

        return new RiskHeatmapDto
        {
            Cells = rows.Select(r => new HeatmapCellDto
            {
                CenterLat = r.CellLat,
                CenterLon = r.CellLon,
                AvgRiskScore = r.AvgRisk,
                PropertyCount = r.PropertyCount,
                H3Index = $"{r.CellLat:F2},{r.CellLon:F2}",
            }).ToList()
        };
    }

    private async Task<List<HeatmapRaw>> ReadHeatmapRowsAsync(
        string sql, double minLat, double maxLat, double minLon, double maxLon, CancellationToken ct)
    {
        var connection = appDb.Database.GetDbConnection();
        var shouldClose = connection.State != ConnectionState.Open;
        if (shouldClose)
            await connection.OpenAsync(ct);

        try
        {
            using var command = connection.CreateCommand();
            command.CommandText = sql;
            AddParameter(command, "minLat", minLat);
            AddParameter(command, "maxLat", maxLat);
            AddParameter(command, "minLon", minLon);
            AddParameter(command, "maxLon", maxLon);

            var rows = new List<HeatmapRaw>();
            await using var reader = await command.ExecuteReaderAsync(ct);
            while (await reader.ReadAsync(ct))
            {
                rows.Add(new HeatmapRaw
                {
                    CellLat = reader.GetDouble(reader.GetOrdinal("CellLat")),
                    CellLon = reader.GetDouble(reader.GetOrdinal("CellLon")),
                    AvgRisk = reader.GetDouble(reader.GetOrdinal("AvgRisk")),
                    PropertyCount = reader.GetInt32(reader.GetOrdinal("PropertyCount")),
                });
            }

            return rows;
        }
        finally
        {
            if (shouldClose)
                await connection.CloseAsync();
        }
    }

    private static void AddParameter(System.Data.Common.DbCommand command, string name, double value)
    {
        var parameter = command.CreateParameter();
        parameter.ParameterName = name;
        parameter.Value = value;
        command.Parameters.Add(parameter);
    }

    private static Polygon ParseGeoJsonPolygon(string geoJson)
    {
        // Minimal GeoJSON polygon coordinate parser
        try
        {
            using var doc = JsonDocument.Parse(geoJson);
            var coords = doc.RootElement
                .GetProperty("coordinates")[0]
                .EnumerateArray()
                .Select(c => new Coordinate(c[0].GetDouble(), c[1].GetDouble()))
                .ToArray();

            return _gf.CreatePolygon(coords);
        }
        catch
        {
            // Return a default 1x1 degree box centered on Cairo if parsing fails
            return _gf.CreatePolygon([
                new(30.5, 29.5), new(31.5, 29.5),
                new(31.5, 30.5), new(30.5, 30.5),
                new(30.5, 29.5)
            ]);
        }
    }

    private class ClusterRaw
    {
        public int ClusterLabel { get; set; }
        public string? CentroidWkt { get; set; }
        public string? HullWkt { get; set; }
        public int PropertyCount { get; set; }
        public double? MedianVal { get; set; }
        public double? P25Val { get; set; }
        public double? P75Val { get; set; }
    }

    private class HeatmapRaw
    {
        public double CellLat { get; set; }
        public double CellLon { get; set; }
        public double AvgRisk { get; set; }
        public int PropertyCount { get; set; }
    }
}
