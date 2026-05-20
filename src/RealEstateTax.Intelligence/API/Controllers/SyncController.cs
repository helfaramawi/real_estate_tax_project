using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.FeatureManagement.Mvc;
using RealEstateTax.Intelligence.Application.DTOs;
using RealEstateTax.Intelligence.Application.Interfaces;
using System.Security.Claims;

namespace RealEstateTax.Intelligence.API.Controllers;

/// <summary>Offline GPS and survey data synchronization from mobile devices.</summary>
[ApiController]
[Route("api/v2/sync")]
[Authorize]
[Produces("application/json")]
public class SyncController(IOfflineSyncService syncService) : ControllerBase
{
    /// <summary>Upload a batch of offline sync packets collected while offline.</summary>
    [HttpPost("upload")]
    [FeatureGate("OfflineSync")]
    [Authorize(Roles = "FieldInspector,TaxOfficer,Admin,SuperAdmin")]
    public async Task<IActionResult> Upload([FromBody] UploadSyncPacketsDto dto, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(dto.DeviceId))
            return BadRequest(new { success = false, error = "DeviceId is required" });
        if (dto.Packets.Count == 0)
            return BadRequest(new { success = false, error = "No packets to upload" });
        if (dto.Packets.Count > 1000)
            return BadRequest(new { success = false, error = "Maximum 1000 packets per upload" });

        var inspectorId = Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var uid) ? uid : Guid.Empty;
        var syncId = await syncService.EnqueuePacketsAsync(dto, inspectorId, ct);

        return Accepted(new SyncUploadResultDto
        {
            SyncId = syncId,
            PacketsReceived = dto.Packets.Count,
            Status = "Queued"
        });
    }

    /// <summary>Check the processing status of an uploaded sync batch.</summary>
    [HttpGet("status/{syncId:guid}")]
    [FeatureGate("OfflineSync")]
    [Authorize(Roles = "FieldInspector,TaxOfficer,Admin,SuperAdmin")]
    public async Task<IActionResult> GetStatus(Guid syncId, CancellationToken ct)
    {
        var status = await syncService.GetSyncStatusAsync(syncId, ct);
        return Ok(new { success = true, data = new { syncId, status } });
    }

    /// <summary>Upload GPS track points directly (high-frequency stream).</summary>
    [HttpPost("tracks")]
    [FeatureGate("GpsCapture")]
    [Authorize(Roles = "FieldInspector,TaxOfficer,Admin,SuperAdmin")]
    public async Task<IActionResult> UploadTracks([FromBody] GpsTrackUploadDto dto, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(dto.DeviceId))
            return BadRequest(new { success = false, error = "DeviceId is required" });
        if (dto.Points.Count == 0)
            return BadRequest(new { success = false, error = "No GPS points to upload" });
        if (dto.Points.Count > 5000)
            return BadRequest(new { success = false, error = "Maximum 5000 points per upload" });

        var inspectorId = Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var uid) ? uid : Guid.Empty;
        await syncService.BulkInsertGpsTracksAsync(dto, inspectorId, ct);

        return Ok(new { success = true, data = new { pointsStored = dto.Points.Count } });
    }
}
