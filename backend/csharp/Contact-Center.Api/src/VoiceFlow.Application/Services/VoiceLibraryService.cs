using VoiceFlow.Application.Interfaces;
using VoiceFlow.Application.Interfaces.Messaging;
using VoiceFlow.Contracts.Common;
using VoiceFlow.Contracts.Events;
using VoiceFlow.Contracts.VoiceLibrary;
using VoiceFlow.Core.Common;
using VoiceFlow.Core.Constatnts.RoutingKeys;
using VoiceFlow.Core.Entities;
using VoiceFlow.Core.Interfaces.Repositories;
using VoiceFlow.Core.Interfaces.Services;

namespace VoiceFlow.Application.Services;

public sealed class VoiceLibraryService : IVoiceLibraryService
{
    private readonly IVoiceLibraryRepository _repository;
    private readonly IVoiceLibraryObjectStorage _voiceObjectStorage;
    private readonly ITtsService _tts;
    private readonly ICallPublisher _callPublisher;

    public VoiceLibraryService(IVoiceLibraryRepository repository, IVoiceLibraryObjectStorage voiceObjectStorage, ITtsService tts, ICallPublisher callPublisher)
    {
        _repository = repository;
        _voiceObjectStorage = voiceObjectStorage;
        _tts = tts;
        _callPublisher = callPublisher;
    }

    public async Task<Result<IEnumerable<VoiceLibraryItemResponse>>> GetItemsAsync(string tenantId, CancellationToken cancellationToken = default)
    {
        var items = await _repository.GetByTenantAsync(tenantId, cancellationToken);
        return Result.Success(items.Select(MapToResponse));
    }

    public async Task<Result<VoiceLibraryItemResponse>> GetItemAsync(string id, string tenantId, CancellationToken cancellationToken = default)
    {
        var item = await _repository.GetByIdAsync(id, cancellationToken);
        if (item is null || item.TenantId != tenantId)
            return Result.Failure<VoiceLibraryItemResponse>(Error.NotFound("VoiceLibraryItem", id));

        return MapToResponse(item);
    }

    public async Task<Result<VoiceLibraryItemResponse>> CreateItemAsync(string tenantId, string userId, CreateVoiceLibraryItemRequest request, Stream? fileStream, string? fileName, CancellationToken cancellationToken = default)
    {
        var item = new VoiceLibraryItem
        {
            TenantId = tenantId,
            UserId = userId,
            Name = request.Name,
            Source = request.Source,
            Text = request.Text,
            Language = request.Language,
            Voice = request.Voice,
            Interruptible = request.Interruptible
        };

        if (fileStream is not null && fileName is not null)
        {
            var ext = Path.GetExtension(fileName);
            var leaf = $"{SanitizeFileLeaf(request.Name)}{ext}";
            var contentType = ResolveContentType(fileName);
            var objectKey = await _voiceObjectStorage.UploadAsync(fileStream, tenantId,leaf, contentType, cancellationToken);
            if (objectKey is not null)
            {
                item.FilePath = objectKey;
                await _callPublisher.PublishAsync(new CallTerminatedEvent
                {
                    StoragePath = objectKey,
                    TenantId = tenantId,
                    Timestamp = DateTime.UtcNow,
                    Event = "VoicePublished"
                },CallRoutingKeys.Call, cancellationToken);

            }
                
        }

        await _repository.InsertAsync(item, cancellationToken);
        return MapToResponse(item);
    }

    public async Task<Result<VoiceLibraryItemResponse>> GenerateTtsAsync(string tenantId, string userId, GenerateTtsRequest request, CancellationToken cancellationToken = default)
    {
        await using var audioStream = await _tts.SynthesizeAsync(request.Text, request.Language, request.Voice, cancellationToken);
        var fileName = $"{Guid.NewGuid():N}.wav";
        var objectKey = await _voiceObjectStorage.UploadAsync(audioStream,tenantId, $"tts/{fileName}", "audio/wav", cancellationToken)
                        ?? throw new InvalidOperationException("Voice library storage returned no object key (is S3 configured?).");

        var item = new VoiceLibraryItem
        {
            TenantId = tenantId,
            UserId = userId,
            Name = request.Name,
            Source = "tts",
            Text = request.Text,
            FilePath = objectKey,
            Language = request.Language,
            Voice = request.Voice
        };

        await _repository.InsertAsync(item, cancellationToken);
        return MapToResponse(item);
    }

    public async Task<Result<VoiceLibraryItemResponse>> UpdateItemAsync(string id, string tenantId, UpdateVoiceLibraryItemRequest request, CancellationToken cancellationToken = default)
    {
        var item = await _repository.GetByIdAsync(id, cancellationToken);
        if (item is null || item.TenantId != tenantId)
            return Result.Failure<VoiceLibraryItemResponse>(Error.NotFound("VoiceLibraryItem", id));

        if (request.Name is not null) item.Name = request.Name;
        if (request.Interruptible.HasValue) item.Interruptible = request.Interruptible.Value;

        await _repository.UpdateAsync(item, cancellationToken);
        return MapToResponse(item);
    }

    public async Task<Result> DeleteItemAsync(string id, string tenantId, CancellationToken cancellationToken = default)
    {
        var item = await _repository.GetByIdAsync(id, cancellationToken);
        if (item is null || item.TenantId != tenantId)
            return Result.Failure(Error.NotFound("VoiceLibraryItem", id));

        if (!string.IsNullOrEmpty(item.FilePath))
            await _voiceObjectStorage.DeleteAsync(item.FilePath, cancellationToken);

        await _repository.DeleteAsync(id, cancellationToken);
        return Result.Success();
    }

    public async Task<Result<SignedUrlResponse>> GetSignedUrlAsync(string id, string tenantId, CancellationToken cancellationToken = default)
    {
        var item = await _repository.GetByIdAsync(id, cancellationToken);
        if (item is null || item.TenantId != tenantId)
            return Result.Failure<SignedUrlResponse>(Error.NotFound("VoiceLibraryItem", id));

        if (string.IsNullOrEmpty(item.FilePath))
            return Result.Failure<SignedUrlResponse>(Error.NotFound("AudioFile", id));

        var expiry = TimeSpan.FromMinutes(30);
        var url = await _voiceObjectStorage.GetPresignedDownloadUrlAsync(item.FilePath, expiry, cancellationToken);
        return new SignedUrlResponse { Url = url, ExpiresAt = DateTime.UtcNow.Add(expiry) };
    }

    private static string SanitizeFileLeaf(string name)
    {
        var invalid = Path.GetInvalidFileNameChars();
        var cleaned = string.Concat((name ?? string.Empty).Trim().Select(c => invalid.Contains(c) ? '_' : c));
        return string.IsNullOrEmpty(cleaned) ? "recording" : cleaned;
    }

    private static string ResolveContentType(string fileName)
    {
        return Path.GetExtension(fileName).ToLowerInvariant() switch
        {
            ".wav" => "audio/wav",
            ".mp3" => "audio/mpeg",
            ".ogg" => "audio/ogg",
            ".webm" => "audio/webm",
            ".m4a" => "audio/mp4",
            ".flac" => "audio/flac",
            _ => "application/octet-stream"
        };
    }

    private static VoiceLibraryItemResponse MapToResponse(VoiceLibraryItem item) => new()
    {
        Id = item.Id,
        Name = item.Name,
        Source = item.Source,
        Text = item.Text,
        Url = item.Url,
        Language = item.Language,
        Voice = item.Voice,
        Interruptible = item.Interruptible,
        Duration = item.Duration,
        FilePath = item.FilePath,
        CreatedAt = item.CreatedAt
    };
}
