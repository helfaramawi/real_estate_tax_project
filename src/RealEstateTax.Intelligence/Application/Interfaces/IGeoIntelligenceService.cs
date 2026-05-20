using RealEstateTax.Intelligence.Application.DTOs;
using RealEstateTax.Intelligence.Domain.Entities;

namespace RealEstateTax.Intelligence.Application.Interfaces;

public interface IGeoIntelligenceService
{
    Task<List<GeoCluster>> GetClustersAsync(string? governorate, CancellationToken ct = default);
    Task RunClusteringAsync(CancellationToken ct = default);
    Task<List<SpatialAnomaly>> GetAnomaliesAsync(string? status, string? severity, CancellationToken ct = default);
    Task<SpatialAnomaly?> UpdateAnomalyStatusAsync(Guid id, string status, string? notes, Guid reviewerId, CancellationToken ct = default);
    Task<List<GeoFenceZone>> GetFenceZonesAsync(bool activeOnly, CancellationToken ct = default);
    Task<GeoFenceZone> CreateFenceZoneAsync(CreateGeoFenceZoneDto dto, CancellationToken ct = default);
    Task UpdatePropertyGeoFenceMembershipAsync(CancellationToken ct = default);
    Task<RiskHeatmapDto> GetRiskHeatmapAsync(double minLat, double minLon, double maxLat, double maxLon, CancellationToken ct = default);
}
