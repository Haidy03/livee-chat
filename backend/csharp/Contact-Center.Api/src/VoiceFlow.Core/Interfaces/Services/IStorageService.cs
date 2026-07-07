namespace VoiceFlow.Core.Interfaces.Services;

public interface IStorageService
{
    Task<string> UploadAsync(Stream content, string fileName, string contentType, CancellationToken cancellationToken = default);
    Task<string> GetSignedUrlAsync(string filePath, TimeSpan expiry, CancellationToken cancellationToken = default);
    Task DeleteAsync(string filePath, CancellationToken cancellationToken = default);
}
