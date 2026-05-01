using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.FeatureManagement.Mvc;
using RealEstateTax.Intelligence.Application.DTOs;
using RealEstateTax.Intelligence.Application.Interfaces;
using System.Security.Claims;

namespace RealEstateTax.Intelligence.API.Controllers;

/// <summary>Geospatial intelligence: clusters, anomalies, geo-fencing, heatmaps.</summary>
[ApiController]
[Route("api/v2/geo")]
[Authorize]
[Produces("application/json")]
public class GeoController(IGeoIntelligenceService geoService) : ControllerBase
{
    /// <summary>Get geospatial property clusters.</summary>
    [HttpGet("clusters")]
    [FeatureGate("GeoClusteringDashboard")]
    [Authorize(Roles = "Admin,SuperAdmin,TaxOfficer")]
    public async Task<IActionResult> GetClusters(
        [FromQuery] string? governorate = null,
        CancellationToken ct = default)
    {
        var clusters = await geoService.GetClustersAsync(governorate, ct);
        return Ok(new { success = true, data = clusters });
    }

    /// <summary>Get detected spatial anomalies.</summary>
    [HttpGet("anomalies")]
    [FeatureGate("GeoClusteringDashboard")]
    [Authorize(Roles = "Admin,SuperAdmin,TaxOfficer")]
    public async Task<IActionResult> GetAnomalies(
        [FromQuery] string? status = "Open",
        [FromQuery] string? severity = null,
        CancellationToken ct = default)
    {
        var anomalies = await geoService.GetAnomaliesAsync(status, severity, ct);
        return Ok(new { success = true, data = anomalies });
    }

    /// <summary>Update the status of a spatial anomaly.</summary>
    [HttpPatch("anomalies/{id:guid}/status")]
    [FeatureGate("GeoClusteringDashboard")]
    [Authorize(Roles = "Admin,SuperAdmin,TaxOfficer")]
    public async Task<IActionResult> UpdateAnomalyStatus(
        Guid id,
        [FromBody] UpdateAnomalyStatusDto dto,
        CancellationToken ct)
    {
        var userId = Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var uid) ? uid : Guid.Empty;
        var result = await geoService.UpdateAnomalyStatusAsync(id, dto.Status, dto.Notes, userId, ct);
        if (result is null) return NotFound(new { success = false, error = "Anomaly not found" });
        return Ok(new { success = true, data = result });
    }

    /// <summary>Get active geo-fence zones.</summary>
    [HttpGet("fence-zones")]
    [FeatureGate("GeoFencing")]
    [Authorize(Roles = "Admin,SuperAdmin,TaxOfficer")]
    public async Task<IActionResult> GetFenceZones(
        [FromQuery] bool active = true,
        CancellationToken ct = default)
    {
        var zones = await geoService.GetFenceZonesAsync(active, ct);
        return Ok(new { success = true, data = zones });
    }

    /// <summary>Create a new geo-fence zone.</summary>
    [HttpPost("fence-zones")]
    [FeatureGate("GeoFencing")]
    [Authorize(Roles = "Admin,SuperAdmin")]
    public async Task<IActionResult> CreateFenceZone(
        [FromBody] CreateGeoFenceZoneDto dto,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(dto.Name) || string.IsNullOrWhiteSpace(dto.GeoJson))
            return BadRequest(new { success = false, error = "Name and GeoJson are required" });

        var zone = await geoService.CreateFenceZoneAsync(dto, ct);
        return CreatedAtAction(nameof(GetFenceZones), new { id = zone.Id },
            new { success = true, data = zone });
    }

    /// <summary>Risk heatmap as grid cells within bounding box.</summary>
    [HttpGet("risk-heatmap")]
    [FeatureGate("GeoClusteringDashboard")]
    [Authorize(Roles = "Admin,SuperAdmin,TaxOfficer")]
    public async Task<IActionResult> GetRiskHeatmap(
        [FromQuery] double minLat,
        [FromQuery] double minLon,
        [FromQuery] double maxLat,
        [FromQuery] double maxLon,
        CancellationToken ct = default)
    {
        if (maxLat - minLat > 5 || maxLon - minLon > 5)
            return BadRequest(new { success = false, error = "Bounding box too large (max 5° per dimension)" });

        var heatmap = await geoService.GetRiskHeatmapAsync(minLat, minLon, maxLat, maxLon, ct);
        return Ok(new { success = true, data = heatmap });
    }
}
