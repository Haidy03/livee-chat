using Contact_Center.Worker.Events.CallEvent.Options;
using HelperLib.CloudStorage;
using HelperLib.Options;
using Microsoft.Extensions.Options;
using VoiceFlow.Contracts.Events;
using VoiceFlow.Core.Common;
using VoiceFlow.Core.Interfaces.Repositories;

namespace Contact_Center.Worker.Events.CallEvent.Services;

public sealed class CallTermenatedProcessingService
{
    private readonly ILogger<CallTermenatedProcessingService> _logger;
    private readonly CallConsumerOptions _callConsumerOptions;
    private readonly S3BucketNamesOptions _s3Configuration;
    private readonly S3StorageService _s3Storage;
    private readonly ICallRepository _callRepository;
    private readonly IVoicemailRepository _voicemailRepository;
    private readonly CallAnalysisService _callAnalysisService;

    public CallTermenatedProcessingService(
        ILogger<CallTermenatedProcessingService> logger,
        IOptions<CallConsumerOptions> callConsumerOptions,
        IOptions<S3BucketNamesOptions> s3Configuration,
        S3StorageService s3Storage,
        CallAnalysisService callAnalysisService,
        ICallRepository callRepository,
        IVoicemailRepository voicemailRepository)
    {
        _logger = logger;
        _callConsumerOptions = callConsumerOptions.Value;
        _s3Configuration = s3Configuration.Value;
        _s3Storage = s3Storage;
        _callRepository = callRepository;
        _voicemailRepository = voicemailRepository;
        _callAnalysisService = callAnalysisService;
    }

    public async Task ProcessCallTerminatedAsync(
     CallTerminatedEvent message,
     CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Processing call {CallId} at {Timestamp}",
            message.CallId,
            message.Timestamp);

        if (string.IsNullOrWhiteSpace(message.CallId) || !message.Timestamp.HasValue)
        {
            _logger.LogWarning(
                "Skipping Asterisk recording upload: missing Call Id or Timestamp.");
            return;
        }

        if (string.IsNullOrWhiteSpace(message.TenantId))
        {
            _logger.LogWarning(
                "Skipping Asterisk recording upload: missing TenantId.");
            return;
        }

        var basePath = _callConsumerOptions.AstriskPath.Trim();

        if (string.IsNullOrEmpty(basePath))
        {
            _logger.LogWarning(
                "Skipping Asterisk recording upload: CallConsumer:AstriskPath is empty.");
            return;
        }

        var callId = message.CallId.Trim();
        var timestamp = message.Timestamp.Value;

        var dateFolder = timestamp.ToString("yyyy_MM_dd");
       var dayPath = Path.Combine(basePath, dateFolder);

        if (!Directory.Exists(dayPath))
        {
            _logger.LogWarning(
                "Asterisk date folder not found: {DayPath}",
                dayPath);

            return;
        }

        var recordingFile = FindRecordingFile(dayPath, callId);

        if (recordingFile is null)
        {
            _logger.LogWarning(
                "No recording file matched call {CallId} under {DayPath}",
                callId,
                dayPath);

            return;
        }

        var bucket = _s3Configuration.ContactCenter?.Trim();

        if (string.IsNullOrEmpty(bucket))
        {
            _logger.LogWarning(
                "Skipping upload: S3 DefaultBucket is not configured.");

            return;
        }

        var fileName = Path.GetFileName(recordingFile);

        var key = $"voice-recordings/{message.TenantId}/{dateFolder}/{fileName}";

        var contentType = ResolveContentType(recordingFile);

        await using var stream = File.OpenRead(recordingFile);

        await _s3Storage.UploadAsync(
            bucket,
            key,
            stream,
            contentType,
            cancellationToken);

        var storagePath = $"s3://{bucket}/{key}";

        CallAnalysisResult? analysis = null;
        try
        {
            analysis = await _callAnalysisService.AnalyzeCallAsync(
                recordingFile,
                cancellationToken);

            _logger.LogInformation(
                "Call analysis succeeded for call {CallId}.",
                callId);

        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Call analysis failed for call {CallId}; recording will still be saved.",
                callId);
        }

        var updated = await _callRepository.SetRecordingUrlAsync(
            message.Id,
            storagePath,
            analysis,
            cancellationToken);

        if (!updated)
        {
            _logger.LogWarning(
                "Uploaded recording for call {CallId} but no MongoDB call document was updated (tenant {TenantId}).",
                callId,
                message.TenantId);
        }
        else
        {
            _logger.LogInformation(
                "Uploaded Asterisk recording to {StoragePath} and saved recordingUrl for call {CallId}.",
                storagePath,
                callId);
        }

        try
        {
            File.Delete(recordingFile);
            _logger.LogInformation(
                "Deleted local Asterisk recording {RecordingFile} after S3 upload.",
                recordingFile);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(
                ex,
                "Failed to delete local Asterisk recording {RecordingFile} after S3 upload.",
                recordingFile);
        }
    }

    public async Task ProcessVoicePublishedAsync(
        CallTerminatedEvent message,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Processing VoicePublished for tenant {TenantId}",
            message.TenantId);

        if (string.IsNullOrWhiteSpace(message.TenantId))
        {
            _logger.LogWarning(
                "Skipping voice download: missing TenantId.");
            return;
        }

        if (string.IsNullOrWhiteSpace(message.StoragePath))
        {
            _logger.LogWarning(
                "Skipping voice download: missing StoragePath.");
            return;
        }

        var basePath = _callConsumerOptions.VoiceLibraryPath.Trim();

        if (string.IsNullOrEmpty(basePath))
        {
            _logger.LogWarning(
                "Skipping voice download: CallConsumer:AstriskPath is empty.");
            return;
        }

        var defaultBucket = _s3Configuration.ContactCenter?.Trim();

        if (string.IsNullOrEmpty(defaultBucket))
        {
            _logger.LogWarning(
                "Skipping voice download: S3 ContactCenter bucket is not configured.");
            return;
        }

        if (!TryParseS3Location(message.StoragePath.Trim(), defaultBucket, out var bucket, out var key))
        {
            _logger.LogWarning(
                "Skipping voice download: could not parse StoragePath {StoragePath}.",
                message.StoragePath);
            return;
        }

        var recordName = Path.GetFileName(key);

        if (string.IsNullOrEmpty(recordName))
        {
            _logger.LogWarning(
                "Skipping voice download: StoragePath has no file name segment.");
            return;
        }

        var tenantId = message.TenantId.Trim();
        var tenantPath = Path.Combine(basePath, tenantId);

        Directory.CreateDirectory(tenantPath);

        var localFilePath = Path.Combine(tenantPath, recordName);

        await using (var s3Stream = await _s3Storage.DownloadAsync(bucket, key, cancellationToken))
        await using (var fileStream = File.Create(localFilePath))
        {
            await s3Stream.CopyToAsync(fileStream, cancellationToken);
        }

        _logger.LogInformation(
            "Saved voice recording from s3://{Bucket}/{Key} to {LocalPath}",
            bucket,
            key,
            localFilePath);
    }

    public async Task ProcessVoicemailRecordedAsync(
        CallTerminatedEvent message,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Processing voicemail {Id} owner {OwnerType}:{OwnerId} tenant {TenantId}",
            message.Id,
            message.OwnerType,
            message.OwnerId,
            message.TenantId);

        if (string.IsNullOrWhiteSpace(message.Id) || string.IsNullOrWhiteSpace(message.TenantId))
        {
            _logger.LogWarning("Skipping voicemail: missing Id or TenantId.");
            return;
        }

        if (string.IsNullOrWhiteSpace(message.RecordingPath))
        {
            _logger.LogWarning("Skipping voicemail {Id}: missing RecordingPath.", message.Id);
            return;
        }

        var recordingFile = message.RecordingPath.Trim();

        // The dialplan reports a host-side path (/var/spool/asterisk/monitor/asterisk/<date>/<file>),
        // but this worker sees that tree mounted at CallConsumer:AstriskPath (e.g. /app/asteriskRecordings).
        // Re-root the path onto the mount — same tree the call-recording flow reads — when the raw path
        // isn't directly visible from inside the container.
        if (!File.Exists(recordingFile))
        {
            var basePath = _callConsumerOptions.AstriskPath?.Trim();
            if (!string.IsNullOrEmpty(basePath))
            {
                var fileName = Path.GetFileName(recordingFile);
                var dateFolder = Path.GetFileName(Path.GetDirectoryName(recordingFile) ?? string.Empty);
                var candidate = string.IsNullOrEmpty(dateFolder)
                    ? Path.Combine(basePath, fileName)
                    : Path.Combine(basePath, dateFolder, fileName);

                if (File.Exists(candidate))
                {
                    recordingFile = candidate;
                }
            }
        }

        if (!File.Exists(recordingFile))
        {
            _logger.LogWarning(
                "Voicemail {Id}: recording file not found at {Path}.",
                message.Id,
                recordingFile);
            return;
        }

        var bucket = _s3Configuration.ContactCenter?.Trim();

        if (string.IsNullOrEmpty(bucket))
        {
            _logger.LogWarning("Skipping voicemail {Id}: S3 ContactCenter bucket is not configured.", message.Id);
            return;
        }

        var ownerId = string.IsNullOrWhiteSpace(message.OwnerId) ? "unassigned" : message.OwnerId.Trim();
        var fileName = Path.GetFileName(recordingFile);
        var key = $"voicemails/{message.TenantId}/{ownerId}/{fileName}";
        var contentType = ResolveContentType(recordingFile);

        await using (var stream = File.OpenRead(recordingFile))
        {
            await _s3Storage.UploadAsync(bucket, key, stream, contentType, cancellationToken);
        }

        var storagePath = $"s3://{bucket}/{key}";

        CallAnalysisResult? analysis = null;
        if (message.Transcription)
        {
            try
            {
                analysis = await _callAnalysisService.AnalyzeCallAsync(recordingFile, cancellationToken);
                _logger.LogInformation("Voicemail analysis succeeded for {Id}.", message.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Voicemail analysis failed for {Id}; recording will still be saved.", message.Id);
            }
        }

        var updated = await _voicemailRepository.SetProcessingResultAsync(
            message.Id,
            storagePath,
            analysis,
            cancellationToken);

        if (!updated)
        {
            _logger.LogWarning(
                "Uploaded voicemail recording for {Id} but no MongoDB voicemail document was updated (tenant {TenantId}).",
                message.Id,
                message.TenantId);
        }
        else
        {
            _logger.LogInformation(
                "Uploaded voicemail recording to {StoragePath} and saved for voicemail {Id}.",
                storagePath,
                message.Id);
        }

        try
        {
            File.Delete(recordingFile);
            _logger.LogInformation("Deleted local voicemail recording {RecordingFile} after S3 upload.", recordingFile);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to delete local voicemail recording {RecordingFile} after S3 upload.", recordingFile);
        }
    }

    private static bool TryParseS3Location(
        string storagePath,
        string defaultBucket,
        out string bucket,
        out string key)
    {
        bucket = defaultBucket;
        key = storagePath;

        if (storagePath.StartsWith("s3://", StringComparison.OrdinalIgnoreCase))
        {
            var withoutScheme = storagePath["s3://".Length..];
            var slashIndex = withoutScheme.IndexOf('/');

            if (slashIndex <= 0)
            {
                return false;
            }

            bucket = withoutScheme[..slashIndex];
            key = withoutScheme[(slashIndex + 1)..];
            return !string.IsNullOrWhiteSpace(bucket) && !string.IsNullOrWhiteSpace(key);
        }

        return !string.IsNullOrWhiteSpace(key);
    }

    private static string? FindRecordingFile(string dayPath, string callId)
    {
        foreach (var file in Directory.GetFiles(dayPath))
        {
            var name = Path.GetFileName(file);

            if (name.Contains(callId, StringComparison.OrdinalIgnoreCase))
            {
                return file;
            }
        }

        return null;
    }

    private static string ResolveContentType(string filePath)
    {
        return Path.GetExtension(filePath).ToLowerInvariant() switch
        {
            ".wav" => "audio/wav",
            ".mp3" => "audio/mpeg",
            ".gsm" => "audio/gsm",
            ".ogg" => "audio/ogg",
            ".m4a" or ".mp4" => "audio/mp4",
            ".json" => "application/json",
            ".txt" => "text/plain",
            _ => "application/octet-stream"
        };
    }
}
