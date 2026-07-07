using System.Threading;
using MongoDB.Bson;
using VoiceFlow.Api.Calls;
using VoiceFlow.Application.Interfaces;
using VoiceFlow.Application.Interfaces.Messaging;
using VoiceFlow.Contracts.Calls;
using VoiceFlow.Contracts.Common;
using VoiceFlow.Contracts.Events;
using VoiceFlow.Core.Common;
using VoiceFlow.Core.Constatnts.RoutingKeys;
using VoiceFlow.Core.Entities;
using VoiceFlow.Core.Enums;
using VoiceFlow.Core.Interfaces.Repositories;
using VoiceFlow.Core.Interfaces.Services;

namespace VoiceFlow.Application.Services;

public sealed class CallService : ICallService
{
    private readonly ICallRepository _callRepository;
    private readonly IStorageService _storageService;
    private readonly IAiGatewayService _aiGatewayService;
    private readonly ICallPublisher _callPublisher;
    private readonly IProfileService _profileService;
    private readonly IGroupService _groupService;
    private readonly ITagService _tagService;
   

    public CallService(
        ICallRepository callRepository,
        IStorageService storageService,
        IAiGatewayService aiGatewayService,
        ICallPublisher callPublisher,
        IProfileService profileService,
        IGroupService groupService,
        ITagService tagService)
    {
        _callRepository = callRepository;
        _storageService = storageService;
        _aiGatewayService = aiGatewayService;
        _callPublisher = callPublisher;
        _profileService = profileService;
        _groupService = groupService;
        _tagService = tagService;
    }

    public async Task<Result<PagedResponse<CallResponse>>> SearchCallsAsync(string tenantId, CallSearchRequest request, CancellationToken cancellationToken = default)
    {
        var (items, totalCount) = await _callRepository.SearchAsync(
            tenantId,
            request.Direction,
            request.Status,
            request.From,
            request.To,
            request.Caller,
            request.TagIds,
            request.UserId,
            request.SoftphoneOnly,
            request.Skip,
            request.PageSize,
            cancellationToken);

        var responses = items.Select(MapToResponse).ToList().AsReadOnly();
        return PagedResponse<CallResponse>.Create(responses, request.Page, request.PageSize, totalCount);
    }

    public async Task<Result<CallSearchResponse>> AdvancedSearchCallsAsync(
        string tenantId,
        AdvancedCallSearchRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request.Duration.Enabled && request.Duration.Min > request.Duration.Max)
            return Result.Failure<CallSearchResponse>(Error.Validation("duration", "Duration min cannot exceed max."));
        if (request.HandlingDuration.Enabled && request.HandlingDuration.Min > request.HandlingDuration.Max)
            return Result.Failure<CallSearchResponse>(Error.Validation("handlingDuration", "Handling duration min cannot exceed max."));
        if (request.WaitingDuration.Enabled && request.WaitingDuration.Min > request.WaitingDuration.Max)
            return Result.Failure<CallSearchResponse>(Error.Validation("waitingDuration", "Waiting duration min cannot exceed max."));
        if (request.HoldingDuration.Enabled && request.HoldingDuration.Min > request.HoldingDuration.Max)
            return Result.Failure<CallSearchResponse>(Error.Validation("holdingDuration", "Holding duration min cannot exceed max."));

        var criteria = CallAdvancedSearchMapper.ToCriteria(request);
        var (items, totalCount) = await _callRepository.AdvancedSearchAsync(tenantId, criteria, cancellationToken);
        var records = items.Select(MapToResponse).ToList().AsReadOnly();
        return CallSearchResponse.Create(records, request.Page, request.PageSize, totalCount);
    }

    public async Task<Result<CallFilterOptions>> GetCallFilterOptionsAsync(string tenantId, CancellationToken cancellationToken = default)
    {
        var profilesTask = _profileService.ListProfilesForTenantAsync(tenantId, cancellationToken);
        var groupsTask = _groupService.GetGroupsAsync(tenantId, cancellationToken);
        var tagsTask = _tagService.GetTagsAsync(tenantId, cancellationToken);
        var abandonmentTask = _callRepository.GetDistinctHangupCausesAsync(tenantId, cancellationToken);

        await Task.WhenAll(profilesTask, groupsTask, tagsTask, abandonmentTask);

        var profiles = await profilesTask;
        var groups = await groupsTask;
        var tags = await tagsTask;
        var abandonmentReasons = await abandonmentTask;

        return new CallFilterOptions
        {
            Agents = profiles.Value
                .Where(p => !p.Disabled)
                .Select(p => new CallFilterAgentOption
                {
                    Id = p.UserId,
                    Name = string.IsNullOrWhiteSpace(p.DisplayName)
                        ? p.Email ?? p.UserId
                        : p.DisplayName
                })
                .OrderBy(a => a.Name, StringComparer.OrdinalIgnoreCase)
                .ToList(),
            Groups = groups.Value
                .Select(g => new CallFilterGroupOption { Id = g.Id, Name = g.Name })
                .OrderBy(g => g.Name, StringComparer.OrdinalIgnoreCase)
                .ToList(),
            Tags = tags.Value
                .Select(t => new CallFilterTagOption { Id = t.Id, Label = t.Label, Color = t.Color })
                .OrderBy(t => t.Label, StringComparer.OrdinalIgnoreCase)
                .ToList(),
            AbandonmentReasons = abandonmentReasons,
            HandledByOptions =
            [
                HandledByFilter.Any,
                HandledByFilter.Agent,
                HandledByFilter.Ai,
                HandledByFilter.Ivr
            ]
        };
    }

    public async Task<Result<CallResponse>> GetCallAsync(string callId, string tenantId, CancellationToken cancellationToken = default)
    {
        
        var call = await _callRepository.GetByIdAsync(callId, cancellationToken);
        if (call is null || call.TenantId != tenantId)
            return Result.Failure<CallResponse>(Error.NotFound("Call", callId));

        return MapToResponse(call);
    }

    public async Task<Result<CallResponse>> CreateCallAsync(string tenantId, string userId, CreateCallRequest request, CancellationToken cancellationToken = default)
    {
        var call = new Call
        {
            TenantId = tenantId,
            UserId = userId,
            CallId = request.CallId,
            Direction = request.Direction,
            Status = request.Status,
            StartedAt = request.StartedAt,
            Caller = request.Caller,
            Called = request.Called,
            TotalSeconds = request.TotalSeconds
        };

        await _callRepository.InsertAsync(call, cancellationToken);
        return MapToResponse(call);
    }

    public async Task<Result<CallResponse>> UpdateCallAsync(string callId, string tenantId, UpdateCallRequest request, CancellationToken cancellationToken = default)
    {
        var call = await _callRepository.GetByIdAsync(callId, cancellationToken);
        if (call is null || call.TenantId != tenantId)
            return Result.Failure<CallResponse>(Error.NotFound("Call", callId));

        if (request.Notes is not null) call.Notes = request.Notes;
        if (request.TagIds is not null) call.TagIds = request.TagIds;
        if (request.Summary is not null) call.Summary = request.Summary;
        if (request.SummaryAccuracyFeedback is not null) call.SummaryAccuracyFeedback = request.SummaryAccuracyFeedback;

        await _callRepository.UpdateAsync(call, cancellationToken);
        return MapToResponse(call);
    }

    public async Task<Result> DeleteCallAsync(string callId, string tenantId, CancellationToken cancellationToken = default)
    {
        var call = await _callRepository.GetByIdAsync(callId, cancellationToken);
        if (call is null || call.TenantId != tenantId)
            return Result.Failure(Error.NotFound("Call", callId));

        await _callRepository.DeleteAsync(callId, cancellationToken);
        return Result.Success();
    }

    public async Task<Result<SignedUrlResponse>> GetRecordingUrlAsync(string callId, string tenantId, CancellationToken cancellationToken = default)
    {
        var call = await _callRepository.GetByIdAsync(callId, cancellationToken);
        if (call is null || call.TenantId != tenantId)
            return Result.Failure<SignedUrlResponse>(Error.NotFound("Call", callId));

        if (!call.HasRecording || string.IsNullOrEmpty(call.RecordingUrl))
            return Result.Failure<SignedUrlResponse>(Error.NotFound("Recording", callId));

        var expiry = TimeSpan.FromMinutes(30);
        var url = await _storageService.GetSignedUrlAsync(call.RecordingUrl, expiry, cancellationToken);

        return new SignedUrlResponse { Url = url, ExpiresAt = DateTime.UtcNow.Add(expiry) };
    }

    public async Task<Result<CallResponse>> GenerateSummaryAsync(string callId, string tenantId, GenerateSummaryRequest request, CancellationToken cancellationToken = default)
    {
        var call = await _callRepository.GetByIdAsync(callId, cancellationToken);
        if (call is null || call.TenantId != tenantId)
            return Result.Failure<CallResponse>(Error.NotFound("Call", callId));

        if (string.IsNullOrEmpty(call.FullTranscript))
            return Result.Failure<CallResponse>(Error.Validation("Transcript", "No transcript available for summarization."));

        var summary = await _aiGatewayService.SummarizeAsync(call.FullTranscript, request.Language, cancellationToken);
        if (summary is not null)
        {
            call.Summary = summary;
            call.SummaryLanguage = request.Language;
            await _callRepository.UpdateAsync(call, cancellationToken);
        }

        return MapToResponse(call);
    }

    public async Task<Result<CallResponse>> TranslateSummaryAsync(string callId, string tenantId, TranslateSummaryRequest request, CancellationToken cancellationToken = default)
    {
        var call = await _callRepository.GetByIdAsync(callId, cancellationToken);
        if (call is null || call.TenantId != tenantId)
            return Result.Failure<CallResponse>(Error.NotFound("Call", callId));

        if (string.IsNullOrEmpty(call.Summary))
            return Result.Failure<CallResponse>(Error.Validation("Summary", "No summary available for translation."));

        var translated = await _aiGatewayService.TranslateAsync(call.Summary, request.TargetLanguage, cancellationToken);
        if (translated is not null)
        {
            call.Summary = translated;
            call.SummaryLanguage = request.TargetLanguage;
            await _callRepository.UpdateAsync(call, cancellationToken);
        }

        return MapToResponse(call);
    }

    public async Task<Result<CallResponse>> UpsertSoftphoneCallAsync(string tenantId, string userId, SoftphoneCallUpsertRequest request, CancellationToken cancellationToken = default)
    {
        var existing = await _callRepository.GetByTenantAndExternalCallIdAsync(tenantId, request.SipCallId, cancellationToken);

        Call call;
        if (existing is null)
        {
            call = new Call
            {
                TenantId = tenantId,
                UserId = userId,
                CallId = request.SipCallId,
                Direction = MapSoftphoneDirection(request.Direction),
                Status = MapSoftphoneStatus(request.Status),
                StartedAt = request.StartedAt ?? DateTime.UtcNow,
                Caller = request.Caller ?? string.Empty,
                Called = request.Called ?? string.Empty,
                CallerId = request.CallerId,
                CallerName = request.CallerName,
                CallerExtension = request.CallerExtension,
                CallerIsAiAgent = request.CallerIsAiAgent ?? false,
                CalledId = request.CalledId,
                CalledName = request.CalledName,
                CalledExtension = request.CalledExtension,
                CalledIsAiAgent = request.CalledIsAiAgent ?? false
            };
            ApplySoftphoneUpsert(call, request);
            await _callRepository.InsertAsync(call, cancellationToken);
        }
        else
        {
            ApplySoftphoneUpsert(existing, request);
            await _callRepository.UpdateAsync(existing, cancellationToken);
            call = existing;
        }

        if (MapSoftphoneStatus(request.Status) == CallStatus.Completed)
        {
            await _callPublisher.PublishAsync(new CallTerminatedEvent
            {
                Id = call.Id,
                CallId = request.SipCallId,
                TenantId = tenantId,
                Timestamp = DateTime.UtcNow,
                Event = "CallTerminated"
            },CallRoutingKeys.Call, cancellationToken);
        }

        return MapToResponse(call);
    }

    public async Task<WrapUpCallResponse> SaveWrapUpAsync(string tenantId, WrapUpCallRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(tenantId))
            throw new InvalidOperationException("tenant_required");
        if (request is null)
            throw new InvalidOperationException("request_required");
        if (string.IsNullOrWhiteSpace(request.SipCallId))
            throw new InvalidOperationException("sipCallId_required");
        if (request.WrapUp is null)
            throw new InvalidOperationException("wrapUp_required");
        if (string.IsNullOrWhiteSpace(request.WrapUp.Disposition))
            throw new InvalidOperationException("disposition_required");
        if (request.WrapUp.AcwSeconds < 0)
            throw new InvalidOperationException("acwSeconds_invalid");


        var existing = await _callRepository
            .GetByTenantAndExternalCallIdAsync(tenantId, request.SipCallId, ct);

        if (existing is null && ObjectId.TryParse(request.SipCallId, out _))
            existing = await _callRepository.GetByIdAsync(request.SipCallId, ct);

        if (existing is null) throw new KeyNotFoundException("Call not found.");


        var completedAt = request.WrapUp.CompletedAt == default
            ? DateTime.UtcNow
            : request.WrapUp.CompletedAt.ToUniversalTime();

        var entity = new CallWrapUp
        {
            Disposition = request.WrapUp.Disposition,
            Notes = request.WrapUp.Notes,
            CallbackScheduled = request.WrapUp.CallbackScheduled,
            AcwSeconds = request.WrapUp.AcwSeconds,
            CompletedAt = completedAt,
            AgentId = request.WrapUp.AgentId,
            Status = string.IsNullOrWhiteSpace(request.WrapUp.Status) ? "wrapped" : request.WrapUp.Status,
        };

        existing.WrapUp = entity;

        await _callRepository.UpdateAsync(existing, ct);

        return new WrapUpCallResponse
        {
            SipCallId = existing.CallId ?? existing.Id,
            Status = entity.Status,
            CompletedAt = entity.CompletedAt,
        };
    }



    private static void ApplySoftphoneUpsert(Call call, SoftphoneCallUpsertRequest r)
    {
        if (r.Direction is not null) call.Direction = MapSoftphoneDirection(r.Direction);
        if (r.Status is not null) call.Status = MapSoftphoneStatus(r.Status);
        if (r.Caller is not null) call.Caller = r.Caller;
        if (r.Called is not null) call.Called = r.Called;
        if (r.FromUri is not null) call.FromUri = r.FromUri;
        if (r.FromDisplay is not null) call.FromDisplay = r.FromDisplay;
        if (r.ToUri is not null) call.ToUri = r.ToUri;
        if (r.ToDisplay is not null) call.ToDisplay = r.ToDisplay;
        if (r.StartedAt.HasValue) call.StartedAt = r.StartedAt.Value;
        if (r.AnsweredAt.HasValue) call.AnsweredAt = r.AnsweredAt;
        if (r.EndedAt.HasValue) call.EndedAt = r.EndedAt;
        if (r.HangupCause is not null) call.HangupCause = r.HangupCause;
        //if (r.RecordingUrl is not null) call.RecordingUrl = r.RecordingUrl;
        call.HasRecording = false;
        if (r.RingSeconds.HasValue) call.RingSeconds = r.RingSeconds.Value;
        if (r.HoldSeconds.HasValue) call.HoldSeconds = r.HoldSeconds.Value;
        if (r.ActiveSeconds.HasValue) call.ActiveSeconds = r.ActiveSeconds.Value;
        if (r.TotalHoldSeconds.HasValue) call.TotalHoldSeconds = r.TotalHoldSeconds.Value;
        if (r.TotalSeconds.HasValue) call.TotalSeconds = r.TotalSeconds.Value;
    }

    private static CallDirection MapSoftphoneDirection(string? d) =>
        string.Equals(d, "in", StringComparison.OrdinalIgnoreCase) ? CallDirection.Inbound : CallDirection.Outbound;

    private static CallStatus MapSoftphoneStatus(string? s)
    {
        if (string.IsNullOrWhiteSpace(s)) return CallStatus.Completed;
        return s.ToLowerInvariant() switch
        {
            "ringing" => CallStatus.Ringing,
            "in-progress" => CallStatus.InProgress,
            "completed" => CallStatus.Completed,
            "missed" => CallStatus.Missed,
            "failed" => CallStatus.Failed,
            "rejected" => CallStatus.Rejected,
            "busy" => CallStatus.Busy,
            "voicemail" => CallStatus.Voicemail,
            _ => CallStatus.NoAnswer
        };
    }

    private static CallResponse MapToResponse(Call call) => new()
    {
        Id = call.Id,
        TenantId = call.TenantId,
        UserId = call.UserId,
        CallId = call.CallId,
        Direction = call.Direction,
        Status = call.Status,
        StartedAt = call.StartedAt,
        AnsweredAt = call.AnsweredAt,
        EndedAt = call.EndedAt,
        RingSeconds = call.RingSeconds,
        HoldSeconds = call.HoldSeconds,
        ActiveSeconds = call.ActiveSeconds,
        TotalHoldSeconds = call.TotalHoldSeconds,
        HangupCause = call.HangupCause,
        AgentId = call.AgentId,
        GroupId = call.GroupId,
        Caller = call.Caller,
        Called = call.Called,
        CallerId = call.CallerId,
        CallerName = call.CallerName,
        CallerExtension = call.CallerExtension,
        CallerIsAiAgent = call.CallerIsAiAgent,
        CalledId = call.CalledId,
        CalledName = call.CalledName,
        CalledExtension = call.CalledExtension,
        CalledIsAiAgent = call.CalledIsAiAgent,
        FromUri = call.FromUri,
        FromDisplay = call.FromDisplay,
        ToUri = call.ToUri,
        ToDisplay = call.ToDisplay,
        TagIds = call.TagIds,
        AutoTagIds = call.AutoTagIds,
        TotalSeconds = call.TotalSeconds,
        HasRecording = call.HasRecording,
        RecordingUrl = call.RecordingUrl,
        Notes = call.Notes,
        Inputs = call.Inputs,
        Summary = call.Summary,
        SummaryLanguage = call.SummaryLanguage,
        SummaryAccuracyFeedback = call.SummaryAccuracyFeedback,
        FullTranscript = call.FullTranscript,
        Sentiment = call.Sentiment,
        Segments = call.Segments,
        WrapUp =call.WrapUp is null ? null : new WrapUpDto()
        {
            AcwSeconds = call.WrapUp?.AcwSeconds ?? 0,
            CompletedAt = call.WrapUp?.CompletedAt?? default,
            Disposition = call.WrapUp?.Disposition?? string.Empty,
            AgentId = call.WrapUp?.AgentId?? string.Empty,
            CallbackScheduled = call.WrapUp?.CallbackScheduled?? false,
            Notes = call.WrapUp?.Notes?? string.Empty,
            Status = call.WrapUp?.Status?? string.Empty
        }
    };
}
