namespace RealEstateTax.Application.Common.Interfaces;

public interface IFileStorageService
{
    Task<string> UploadAsync(Stream stream, string fileName, string contentType, string folder, CancellationToken ct = default);
    Task<Stream> DownloadAsync(string storagePath, CancellationToken ct = default);
    Task DeleteAsync(string storagePath, CancellationToken ct = default);
    Task<bool> ExistsAsync(string storagePath, CancellationToken ct = default);
    string GetPublicUrl(string storagePath);
}
