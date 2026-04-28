using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using RealEstateTax.Application.Common.Interfaces;

namespace RealEstateTax.Infrastructure.Services;

/// <summary>
/// Local filesystem storage implementation.
/// TODO: Replace with S3-compatible object storage (MinIO, AWS S3, Azure Blob) for production.
/// </summary>
public class FileStorageService : IFileStorageService
{
    private readonly string _basePath;
    private readonly string _baseUrl;
    private readonly ILogger<FileStorageService> _logger;

    public FileStorageService(IConfiguration config, ILogger<FileStorageService> logger)
    {
        _basePath = config["Storage:LocalPath"] ?? Path.Combine(Path.GetTempPath(), "retax-uploads");
        _baseUrl = config["Storage:BaseUrl"] ?? "/files";
        _logger = logger;
        Directory.CreateDirectory(_basePath);
    }

    public async Task<string> UploadAsync(Stream stream, string fileName, string contentType, string folder, CancellationToken ct = default)
    {
        var sanitized = SanitizeFileName(fileName);
        var uniqueName = $"{Guid.NewGuid():N}_{sanitized}";
        var relativePath = Path.Combine(folder, uniqueName);
        var fullPath = Path.Combine(_basePath, relativePath);

        Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);

        await using var fileStream = new FileStream(fullPath, FileMode.Create, FileAccess.Write);
        await stream.CopyToAsync(fileStream, ct);

        _logger.LogInformation("File uploaded to {Path}", relativePath);
        return relativePath;
    }

    public async Task<Stream> DownloadAsync(string storagePath, CancellationToken ct = default)
    {
        var fullPath = Path.Combine(_basePath, storagePath);
        if (!File.Exists(fullPath))
            throw new FileNotFoundException("File not found", storagePath);

        // Return memory stream so file handle is released immediately
        var memory = new MemoryStream();
        await using var fs = new FileStream(fullPath, FileMode.Open, FileAccess.Read);
        await fs.CopyToAsync(memory, ct);
        memory.Position = 0;
        return memory;
    }

    public Task DeleteAsync(string storagePath, CancellationToken ct = default)
    {
        var fullPath = Path.Combine(_basePath, storagePath);
        if (File.Exists(fullPath)) File.Delete(fullPath);
        return Task.CompletedTask;
    }

    public Task<bool> ExistsAsync(string storagePath, CancellationToken ct = default)
    {
        var fullPath = Path.Combine(_basePath, storagePath);
        return Task.FromResult(File.Exists(fullPath));
    }

    public string GetPublicUrl(string storagePath) =>
        $"{_baseUrl.TrimEnd('/')}/{storagePath.Replace('\\', '/')}";

    private static string SanitizeFileName(string fileName)
    {
        var invalid = Path.GetInvalidFileNameChars();
        return string.Concat(fileName.Select(c => invalid.Contains(c) ? '_' : c));
    }
}
