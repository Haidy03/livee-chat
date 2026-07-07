using Microsoft.Extensions.Logging;
using VoiceFlow.Core.Interfaces.Services;

namespace VoiceFlow.Infrastructure.ExternalServices;

public sealed class AzureBlobStorageService : IStorageService
{
    private readonly ILogger<AzureBlobStorageService> _logger;

    public AzureBlobStorageService(ILogger<AzureBlobStorageService> logger)
    {
        _logger = logger;
    }

    public Task<string> UploadAsync(Stream content, string fileName, string contentType, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Storage: uploading {FileName}", fileName);
        return Task.FromResult($"uploads/{fileName}");
    }

    public Task<string> GetSignedUrlAsync(string filePath, TimeSpan expiry, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Storage: generating signed URL for {FilePath}", filePath);
        return Task.FromResult($"https://storage.example.com/{filePath}?expires={DateTime.UtcNow.Add(expiry):O}");
    }

    public Task DeleteAsync(string filePath, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Storage: deleting {FilePath}", filePath);
        return Task.CompletedTask;
    }
}
