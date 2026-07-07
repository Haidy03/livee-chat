namespace VoiceFlow.Application.Interfaces;

public interface IVoiceLibraryObjectStorage
{
    /// <summary>
    /// Uploads audio under voice-recordings/{yyyy_MM_dd}/{fileLeafName}. Returns null if the bucket is not configured.
    /// </summary>
    Task<string?> UploadAsync(Stream content,string tenantId, string fileLeafName, string contentType, CancellationToken cancellationToken = default);

    Task DeleteAsync(string objectKey, CancellationToken cancellationToken = default);

    Task<string> GetPresignedDownloadUrlAsync(string objectKey, TimeSpan expiry, CancellationToken cancellationToken = default);
}
