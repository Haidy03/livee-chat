using HelperLib.CloudStorage;
using HelperLib.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using VoiceFlow.Application.Interfaces;

namespace VoiceFlow.Infrastructure.ExternalServices;

public sealed class S3VoiceLibraryObjectStorage : IVoiceLibraryObjectStorage
{
    private readonly S3StorageService _s3Storage;
    private readonly S3BucketNamesOptions _bucketNames;
    private readonly ILogger<S3VoiceLibraryObjectStorage> _logger;

    public S3VoiceLibraryObjectStorage(
        S3StorageService s3Storage,
        IOptions<S3BucketNamesOptions> bucketNamesOptions,
        ILogger<S3VoiceLibraryObjectStorage> logger)
    {
        _s3Storage = s3Storage;
        _bucketNames = bucketNamesOptions.Value;
        _logger = logger;
    }

    public async Task<string?> UploadAsync(Stream content,string tenantId, string fileLeafName, string contentType, CancellationToken cancellationToken = default)
    {
        var bucket = _bucketNames.ContactCenter?.Trim();
        if (string.IsNullOrEmpty(bucket))
        {
            _logger.LogWarning("Skipping upload: S3 ContactCenter bucket is not configured.");
            return null;
        }
        var timestamp = DateTime.UtcNow;
        var safeLeaf = SafeRelativeLeaf(fileLeafName);
        var key = $"voicelibrary/{tenantId}/{safeLeaf}";
        await _s3Storage.UploadAsync(bucket, key, content, contentType, cancellationToken);
        return $"s3://{bucket}/{key}";
    }

    public async Task DeleteAsync(string objectKey, CancellationToken cancellationToken = default)
    {
        var bucket = _bucketNames.ContactCenter?.Trim();
        if (string.IsNullOrEmpty(bucket))
        {
            _logger.LogWarning("Skipping delete: S3 ContactCenter bucket is not configured.");
            return;
        }

        await _s3Storage.DeleteAsync(bucket, objectKey, cancellationToken);
    }

    public Task<string> GetPresignedDownloadUrlAsync(string objectKey, TimeSpan expiry, CancellationToken cancellationToken = default)
    {
        var bucket = _bucketNames.ContactCenter?.Trim();
        if (string.IsNullOrEmpty(bucket))
            throw new InvalidOperationException("S3 ContactCenter bucket is not configured.");

        return _s3Storage.GetDownloadUrl(bucket, objectKey, expiry, cancellationToken);
    }

    private static string SafeRelativeLeaf(string fileLeafName)
    {
        var raw = (fileLeafName ?? string.Empty).Trim().Replace('\\', '/');
        if (string.IsNullOrEmpty(raw))
            return "recording";

        var segments = raw.Split('/', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var invalid = Path.GetInvalidFileNameChars();
        var parts = new List<string>();
        foreach (var seg in segments)
        {
            var nameOnly = Path.GetFileName(seg);
            if (string.IsNullOrEmpty(nameOnly))
                continue;

            var cleaned = string.Concat(nameOnly.Select(c => invalid.Contains(c) ? '_' : c));
            if (!string.IsNullOrEmpty(cleaned))
                parts.Add(cleaned);
        }

        return parts.Count == 0 ? "recording" : string.Join('/', parts);
    }
}
