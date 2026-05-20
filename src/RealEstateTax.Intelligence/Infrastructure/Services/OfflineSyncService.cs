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

public class OfflineSyncService(
    IntelligenceDbContext intelDb,
    ILogger<OfflineSyncService> logger) : IOfflineSyncService
{
    private static readonly GeometryFactory _gf = new(new PrecisionModel(), 4326);

    public async Task<Guid> EnqueuePacketsAsync(
        UploadSyncPacketsDto dto, Guid inspectorId, CancellationToken ct = default)
    {
        // Use a shared sync batch ID stored in the first packet's ID for tracking
        var batchId = Guid.NewGuid();
        var packets = dto.Packets.Select(p => new OfflineSyncPacket
        {
            Id = p == dto.Packets.First() ? batchId : Guid.NewGuid(),
            DeviceId = dto.DeviceId,
            InspectorId = inspectorId,
            PacketType = Enum.TryParse<SyncPacketType>(p.Type, true, out var t) ? t : SyncPacketType.GpsTrack,
            Payload = JsonSerializer.Serialize(p.Payload),
            Status = "Pending",
        }).ToList();

        intelDb.OfflineSyncQueue.AddRange(packets);
        await intelDb.SaveChangesAsync(ct);
        logger.LogInformation("Enqueued {Count} offline sync packets, batch {BatchId}", packets.Count, batchId);
        return batchId;
    }

    public async Task<string> GetSyncStatusAsync(Guid syncId, CancellationToken ct = default)
    {
        var packet = await intelDb.OfflineSyncQueue.FindAsync([syncId], ct);
        return packet?.Status ?? "NotFound";
    }

    public async Task ProcessPendingPacketsAsync(CancellationToken ct = default)
    {
        var pending = await intelDb.OfflineSyncQueue
            .Where(p => p.Status == "Pending" && p.RetryCount < 3)
            .OrderBy(p => p.SubmittedAt)
            .Take(100)
            .ToListAsync(ct);

        foreach (var packet in pending)
        {
            try
            {
                packet.Status = "Processing";
                await intelDb.SaveChangesAsync(ct);

                if (packet.PacketType == SyncPacketType.GpsTrack)
                    await ProcessGpsTrackPacketAsync(packet, ct);

                packet.Status = "Applied";
                packet.ProcessedAt = DateTime.UtcNow;
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Failed to process sync packet {Id}, retry {Count}", packet.Id, packet.RetryCount);
                packet.Status = packet.RetryCount >= 2 ? "Failed" : "Pending";
                packet.RetryCount++;
                packet.ErrorMessage = ex.Message;
            }
            await intelDb.SaveChangesAsync(ct);
        }
    }

    public async Task BulkInsertGpsTracksAsync(GpsTrackUploadDto dto, Guid inspectorId, CancellationToken ct = default)
    {
        // Validate timestamps — reject points older than 24 hours (anti-replay)
        var cutoff = DateTime.UtcNow.AddHours(-24);
        var validPoints = dto.Points
            .Where(p => p.RecordedAt >= cutoff && p.RecordedAt <= DateTime.UtcNow)
            .ToList();

        if (validPoints.Count == 0) return;

        var tracks = validPoints.Select(p => new InspectorTrack
        {
            InspectorId = inspectorId,
            FieldSurveyId = dto.FieldSurveyId,
            RecordedAt = p.RecordedAt,
            Location = _gf.CreatePoint(new Coordinate(p.Lon, p.Lat)),
            AccuracyMeters = p.AccuracyMeters,
            SpeedKmh = p.SpeedKmh,
            HeadingDegrees = p.HeadingDegrees,
            AltitudeM = p.AltitudeM,
            BatteryLevel = p.BatteryLevel,
            DeviceId = dto.DeviceId,
            IsOfflineRecord = false,
        }).ToList();

        intelDb.InspectorTracks.AddRange(tracks);
        await intelDb.SaveChangesAsync(ct);
    }

    private async Task ProcessGpsTrackPacketAsync(OfflineSyncPacket packet, CancellationToken ct)
    {
        var trackDto = JsonSerializer.Deserialize<GpsTrackUploadDto>(packet.Payload);
        if (trackDto is null) return;

        var cutoff = DateTime.UtcNow.AddHours(-48);
        var tracks = trackDto.Points
            .Where(p => p.RecordedAt >= cutoff)
            .Select(p => new InspectorTrack
            {
                InspectorId = packet.InspectorId,
                FieldSurveyId = trackDto.FieldSurveyId,
                RecordedAt = p.RecordedAt,
                Location = _gf.CreatePoint(new Coordinate(p.Lon, p.Lat)),
                AccuracyMeters = p.AccuracyMeters,
                SpeedKmh = p.SpeedKmh,
                DeviceId = packet.DeviceId,
                IsOfflineRecord = true,
            }).ToList();

        intelDb.InspectorTracks.AddRange(tracks);
    }
}
