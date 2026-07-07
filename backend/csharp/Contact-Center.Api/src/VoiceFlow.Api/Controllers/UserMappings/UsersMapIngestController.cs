using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VoiceFlow.Api.Filters;
using VoiceFlow.Application.Helpers;
using VoiceFlow.Contracts.Common;
using VoiceFlow.Contracts.UserMaps.Requests;
using VoiceFlow.Core.Helpers;
using VoiceFlow.Core.Interfaces.Repositories;
using VoiceFlow.Core.Interfaces.Services;
using VoiceFlow.Core.Models;
using VoiceFlow.Reports.Core.Entities;

namespace VoiceFlow.Reports.Api.Controllers.UserMappings;

/// <summary>
/// Dialplan-facing ingest API. The Asterisk/FreeSWITCH dialplans POST to
/// /state on entry to every IVR component, /end on call termination, and
/// /metric for periodic aggregate pushes from the reports pipeline.
/// </summary>
[ApiController]
[AllowAnonymous]
[IngestTokenAuth]
[Route("api/v1/live/users-map/ingest")]
[Produces("application/json")]
public sealed class UsersMapIngestController : ControllerBase
{
    private readonly ILiveCallRegistry _registry;
    private readonly ILiveCallRecordRepository _callRecords;
    private readonly ILogger<UsersMapIngestController> _log;

    public UsersMapIngestController(
        ILiveCallRegistry registry,
        ILiveCallRecordRepository callRecords,
        ILogger<UsersMapIngestController> log)
    {
        _registry = registry;
        _callRecords = callRecords;
        _log = log;
    }

    [HttpPost("state")]
    public async Task<ActionResult<ApiResponse<object>>> State([FromBody] IngestStateRequest body, CancellationToken ct)
    {
        if (body == null || string.IsNullOrWhiteSpace(body.CallId))
            return BadRequest(new { error = "callId required" });

        var nowIso = DateTimeOffset.UtcNow.ToString("O");
        var atIso = string.IsNullOrEmpty(body.At) ? nowIso : body.At;
        var state = CallStateExtensions.TryParseWire(body.State, out var parsed)
            ? parsed
            : NodeKindMap.ToState(body.Node?.Kind, _log);

        var call = new LiveCallRecord
        {
            Id = body.CallId,
            CallId = body.CallId,
            TenantId = body.TenantId,
            Name = body.Caller?.Name ?? "Unknown",
            MaskedNumber = body.Caller?.Number ?? string.Empty,
            Color = body.Color ?? "#647590",
            State = state,
            EnteredStateAt = atIso,
            CallStartedAt = atIso, // overwritten below when an earlier value exists
            FlowId = body.Node?.FlowId,
            NodeKey = body.Node?.Key,
            NodeKind = body.Node?.Kind,
            NodeLabel = body.Node?.Label,
            IvrChoice = body.IvrChoice,
            Intent = body.Intent,
            Agent = body.Agent == null ? null : new LiveAgentRef { Id = body.Agent.Id, Name = body.Agent.Name },
            QueuePosition = body.QueuePosition,
            SurveyStep = body.Survey?.Step,
            SurveyTotal = body.Survey?.Total,
            Channel = body.Channel ?? "voice",
            Direction = body.Direction ?? "inbound",
            Tags = body.Tags,
            History = new List<LiveJourneyStep>(),
        };

        // Preserve call-start timestamp + history across transitions by reading the prior record.
        var existing = await ReadExistingAsync(body.TenantId, body.CallId, ct);
        if (existing != null)
        {
            call.CallStartedAt = existing.CallStartedAt;
            call.History = existing.History ?? new List<LiveJourneyStep>();
            // Carry forward fields that aren't re-asserted on this event.
            if (string.IsNullOrEmpty(call.Name) || call.Name == "Unknown") call.Name = existing.Name;
            if (string.IsNullOrEmpty(call.MaskedNumber)) call.MaskedNumber = existing.MaskedNumber;
            if (string.IsNullOrEmpty(call.Color) || call.Color == "#647590") call.Color = existing.Color;
            if (call.Agent == null) call.Agent = existing.Agent;
            if (call.Tags == null) call.Tags = existing.Tags;
        }

        call.History.Add(new LiveJourneyStep
        {
            At = atIso,
            NodeKey = body.Node?.Key ?? state.ToWire(),
            Kind = body.Node?.Kind,
            Label = body.Node?.Label ?? body.Node?.Key ?? state.ToWire(),
            Sub = body.IvrChoice ?? body.Intent,
        });

        // Terminal state: persist a finalized CallRecord to Mongo and drop
        // the call from Redis so it disappears from the live Users Map.
        if (state == CallState.EndCall)
        {
            var record = BuildRecord(call, atIso, "completed");
            try
            {
                await _callRecords.AddAsync(record, ct);
            }
            catch (Exception ex)
            {
                _log.LogError(ex, "Failed to persist CallRecord for {CallId}", call.CallId);
            }
            await _registry.RemoveAsync(call.TenantId, call.CallId, "completed", ct);
            return Ok(ApiResponse<object>.Ok(new { ok = true, finalized = true }));
        }

        await _registry.RecordStateAsync(call, ct);
        return Ok(ApiResponse<object>.Ok(new { ok = true }));
    }

    private static LiveCall BuildRecord(LiveCallRecord call, string endedAtIso, string reason)
    {
        var durationSec = 0;
        if (DateTimeOffset.TryParse(call.CallStartedAt, out var started) &&
            DateTimeOffset.TryParse(endedAtIso, out var ended))
        {
            durationSec = (int)Math.Max(0, (ended - started).TotalSeconds);
        }

        return new LiveCall
        {
            TenantId = call.TenantId,
            CallId = call.CallId,
            Name = call.Name,
            MaskedNumber = call.MaskedNumber,
            Color = call.Color,
            FinalState = call.State.ToWire(),
            Reason = reason,
            CallStartedAt = call.CallStartedAt,
            EndedAt = endedAtIso,
            DurationSec = durationSec,
            FlowId = call.FlowId,
            NodeKey = call.NodeKey,
            NodeKind = call.NodeKind,
            NodeLabel = call.NodeLabel,
            IvrChoice = call.IvrChoice,
            Intent = call.Intent,
            Detail = call.Detail,
            Agent = call.Agent,
            QueuePosition = call.QueuePosition,
            SurveyStep = call.SurveyStep,
            SurveyTotal = call.SurveyTotal,
            Channel = call.Channel,
            Direction = call.Direction,
            Tags = call.Tags,
            History = call.History ?? new List<LiveJourneyStep>(),
        };
    }

    [HttpPost("end")]
    public async Task<ActionResult<ApiResponse<object>>> End([FromBody] IngestEndRequest body, CancellationToken ct)
    {
        if (body == null || string.IsNullOrWhiteSpace(body.CallId))
            return BadRequest(new { error = "callId required" });
        await _registry.RemoveAsync(body.TenantId, body.CallId, body.Reason ?? "completed", ct);
        return Ok(ApiResponse<object>.Ok(new { ok = true }));
    }

    [HttpPost("metric")]
    public async Task<ActionResult<ApiResponse<object>>> Metric([FromBody] IngestMetricRequest body, CancellationToken ct)
    {
        if (body == null) return BadRequest();
        await _registry.UpdateMetricsAsync(body.TenantId, body.AvgHandleSec, body.SlaPercent, body.SlaTargetPercent, ct);
        return Ok(ApiResponse<object>.Ok(new { ok = true }));
    }

    private async Task<LiveCallRecord?> ReadExistingAsync(string tenantId, string callId, CancellationToken ct)
    {
        try
        {
            var snap = await _registry.GetSnapshotAsync(tenantId, ct);
            return snap.Calls.FirstOrDefault(c => c.CallId == callId);
        }
        catch
        {
            return null;
        }
    }
}
