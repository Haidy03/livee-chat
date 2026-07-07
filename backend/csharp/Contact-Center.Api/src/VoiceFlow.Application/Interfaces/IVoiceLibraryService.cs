using VoiceFlow.Contracts.Common;
using VoiceFlow.Contracts.VoiceLibrary;
using VoiceFlow.Core.Common;

namespace VoiceFlow.Application.Interfaces;

public interface IVoiceLibraryService
{
    Task<Result<IEnumerable<VoiceLibraryItemResponse>>> GetItemsAsync(string tenantId, CancellationToken cancellationToken = default);
    Task<Result<VoiceLibraryItemResponse>> GetItemAsync(string id, string tenantId, CancellationToken cancellationToken = default);
    Task<Result<VoiceLibraryItemResponse>> CreateItemAsync(string tenantId, string userId, CreateVoiceLibraryItemRequest request, Stream? fileStream, string? fileName, CancellationToken cancellationToken = default);
    Task<Result<VoiceLibraryItemResponse>> GenerateTtsAsync(string tenantId, string userId, GenerateTtsRequest request, CancellationToken cancellationToken = default);
    Task<Result<VoiceLibraryItemResponse>> UpdateItemAsync(string id, string tenantId, UpdateVoiceLibraryItemRequest request, CancellationToken cancellationToken = default);
    Task<Result> DeleteItemAsync(string id, string tenantId, CancellationToken cancellationToken = default);
    Task<Result<SignedUrlResponse>> GetSignedUrlAsync(string id, string tenantId, CancellationToken cancellationToken = default);
}
