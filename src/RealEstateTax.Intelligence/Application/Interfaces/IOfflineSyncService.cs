using RealEstateTax.Intelligence.Application.DTOs;

namespace RealEstateTax.Intelligence.Application.Interfaces;

public interface IOfflineSyncService
{
    Task<Guid> EnqueuePacketsAsync(UploadSyncPacketsDto dto, Guid inspectorId, CancellationToken ct = default);
    Task<string> GetSyncStatusAsync(Guid syncId, CancellationToken ct = default);
    Task ProcessPendingPacketsAsync(CancellationToken ct = default);
    Task BulkInsertGpsTracksAsync(GpsTrackUploadDto dto, Guid inspectorId, CancellationToken ct = default);
}
