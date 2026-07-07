using VoiceFlow.Application.Interfaces;
using VoiceFlow.Contracts.Calls;
using VoiceFlow.Core.Common;
using VoiceFlow.Core.Enums;
using VoiceFlow.Core.Models;

namespace VoiceFlow.Application.Services;

internal static class CallAdvancedSearchMapper
{
    public static CallAdvancedSearchCriteria ToCriteria(AdvancedCallSearchRequest request)
    {
        var (dateFrom, dateTo) = ResolveDateRange(request);

        return new CallAdvancedSearchCriteria
        {
            DateFrom = dateFrom,
            DateTo = dateTo,
            Directions = MapDirections(request.Types),
            Statuses = ParseStatuses(request.Statuses),
            DirectionStates = BuildDirectionStates(request),
            HasRecording = request.Properties.Contains(CallPropertyFilter.Recording) ? true : null,
            HasVoicemail = request.Properties.Contains(CallPropertyFilter.Voicemail) ? true : null,
            HasTransfer = request.Properties.Contains(CallPropertyFilter.Transfer) ? true : null,
            HasHold = request.Properties.Contains(CallPropertyFilter.Hold) ? true : null,
            Sentiments = request.Sentiment.Count > 0 ? request.Sentiment : null,
            HangUpByAgent = request.HangUpByAgent,
            AbandonmentReasons = NullIfEmpty(request.AbandonmentReasons),
            AgentIds = NullIfEmpty(request.AgentIds),
            GroupIds = NullIfEmpty(request.GroupIds),
            TagIds = NullIfEmpty(request.TagIds),
            Caller = NullIfWhiteSpace(request.Caller),
            Callee = NullIfWhiteSpace(request.Callee),
            HandledBy = MapHandledBy(request.HandledBy),
            CallId = NullIfWhiteSpace(request.CallId),
            ReferenceId = NullIfWhiteSpace(request.ReferenceId),
            Keyword = NullIfWhiteSpace(request.Keyword),
            SearchOperator = MapSearchOperator(request.SearchOperator),
            Duration = MapDuration(request.Duration),
            HandlingDuration = MapDuration(request.HandlingDuration),
            WaitingDuration = MapDuration(request.WaitingDuration),
            HoldingDuration = MapDuration(request.HoldingDuration),
            SortBy = request.SortBy,
            SortDescending = request.SortDir == SortDirectionFilter.Desc,
            Skip = request.Skip,
            Take = request.PageSize
        };
    }

    public static CallRecord ToRecord(Core.Entities.Call call)
    {
        var properties = new List<CallPropertyFilter>();
        if (call.HasRecording) properties.Add(CallPropertyFilter.Recording);
        if (call.Status == CallStatus.Voicemail) properties.Add(CallPropertyFilter.Voicemail);
        if (call.HoldSeconds > 0 || call.TotalHoldSeconds > 0) properties.Add(CallPropertyFilter.Hold);
        if (call.HangupCause?.Contains("transfer", StringComparison.OrdinalIgnoreCase) == true)
            properties.Add(CallPropertyFilter.Transfer);

        return new CallRecord
        {
            Id = call.Id,
            Direction = MapDirectionToFilter(call),
            Status = ToStatusString(call.Status),
            StartedAt = call.StartedAt,
            RingSeconds = call.RingSeconds,
            HoldSeconds = call.HoldSeconds,
            ActiveSeconds = call.ActiveSeconds,
            TotalSeconds = call.TotalSeconds,
            AgentId = call.AgentId,
            GroupId = call.GroupId,
            Caller = call.Caller,
            Called = call.Called,
            TagIds = call.TagIds,
            AutoTagIds = call.AutoTagIds,
            Sentiment = call.Sentiment,
            HandledBy = ResolveHandledBy(call),
            ReferenceId = call.CallId,
            Inputs = call.Inputs,
            HasRecording = call.HasRecording,
            Properties = properties,
            AbandonmentReason = ResolveAbandonmentReason(call),
            Notes = call.Notes
        };
    }

    private static (DateTime? From, DateTime? To) ResolveDateRange(AdvancedCallSearchRequest request)
    {
        if (request.DateFrom.HasValue || request.DateTo.HasValue)
            return (request.DateFrom, request.DateTo);

        var utcNow = DateTime.UtcNow;
        return request.DateRange.Trim().ToLowerInvariant() switch
        {
            "today" => (utcNow.Date, utcNow),
            "yesterday" => (utcNow.Date.AddDays(-1), utcNow.Date.AddTicks(-1)),
            "7d" => (utcNow.Date.AddDays(-7), utcNow),
            "30d" => (utcNow.Date.AddDays(-30), utcNow),
            _ => (null, null)
        };
    }

    private static IReadOnlyList<CallDirection>? MapDirections(IReadOnlyList<CallTypeFilter> types)
    {
        if (types.Count == 0)
            return null;

        var directions = types
            .Where(t => t != CallTypeFilter.Self)
            .Select(t => t switch
            {
                CallTypeFilter.Inbound => CallDirection.Inbound,
                CallTypeFilter.Outbound => CallDirection.Outbound,
                CallTypeFilter.Internal => CallDirection.Internal,
                _ => (CallDirection?)null
            })
            .Where(d => d.HasValue)
            .Select(d => d!.Value)
            .Distinct()
            .ToList();

        return directions.Count > 0 ? directions : null;
    }

    private static IReadOnlyList<(CallDirection Direction, IReadOnlyList<CallStatus> Statuses)>? BuildDirectionStates(
        AdvancedCallSearchRequest request)
    {
        var directionStates = new List<(CallDirection, IReadOnlyList<CallStatus>)>();

        AddDirectionState(directionStates, CallDirection.Inbound, request.InboundStates);
        AddDirectionState(directionStates, CallDirection.Outbound, request.OutboundStates);
        AddDirectionState(directionStates, CallDirection.Outbound, request.CampaignStates);
        AddDirectionState(directionStates, CallDirection.Internal, request.InternalStates);

        if (directionStates.Count == 0)
            return null;

        if (request.Types.Count > 0)
        {
            var allowedDirections = request.Types
                .Select(t => t switch
                {
                    CallTypeFilter.Inbound => CallDirection.Inbound,
                    CallTypeFilter.Outbound => CallDirection.Outbound,
                    CallTypeFilter.Internal => CallDirection.Internal,
                    CallTypeFilter.Self => CallDirection.Internal,
                    _ => (CallDirection?)null
                })
                .Where(d => d.HasValue)
                .Select(d => d!.Value)
                .ToHashSet();

            directionStates = directionStates
                .Where(ds => allowedDirections.Contains(ds.Item1))
                .ToList();
        }

        return directionStates.Count > 0 ? directionStates : null;
    }

    private static void AddDirectionState(
        List<(CallDirection, IReadOnlyList<CallStatus>)> target,
        CallDirection direction,
        IReadOnlyList<string> states)
    {
        var parsed = ParseStatuses(states);
        if (parsed?.Count > 0)
            target.Add((direction, parsed));
    }

    private static IReadOnlyList<CallStatus>? ParseStatuses(IReadOnlyList<string>? values)
    {
        if (values is null || values.Count == 0)
            return null;

        var statuses = values
            .Select(ParseStatus)
            .Where(s => s.HasValue)
            .Select(s => s!.Value)
            .Distinct()
            .ToList();

        return statuses.Count > 0 ? statuses : null;
    }

    private static CallStatus? ParseStatus(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        var normalized = value.Trim().Replace("-", string.Empty, StringComparison.Ordinal);
        return Enum.TryParse<CallStatus>(normalized, true, out var status) ? status : null;
    }

    private static DurationRangeCriteria? MapDuration(DurationFilter? filter)
    {
        if (filter is null || !filter.Enabled)
            return null;

        return new DurationRangeCriteria
        {
            Min = filter.Min,
            Max = filter.Max
        };
    }

    private static HandledByCriteria MapHandledBy(HandledByFilter handledBy) =>
        handledBy switch
        {
            HandledByFilter.Agent => HandledByCriteria.Agent,
            HandledByFilter.Ai => HandledByCriteria.Ai,
            HandledByFilter.Ivr => HandledByCriteria.Ivr,
            _ => HandledByCriteria.Any
        };

    private static SearchOperatorCriteria MapSearchOperator(SearchOperatorFilter searchOperator) =>
        searchOperator switch
        {
            SearchOperatorFilter.Or => SearchOperatorCriteria.Or,
            SearchOperatorFilter.Phrase => SearchOperatorCriteria.Phrase,
            _ => SearchOperatorCriteria.And
        };

    private static CallTypeFilter MapDirectionToFilter(Core.Entities.Call call)
    {
        if (call.Direction == CallDirection.Internal &&
            string.Equals(call.Caller, call.Called, StringComparison.OrdinalIgnoreCase))
            return CallTypeFilter.Self;

        return call.Direction switch
        {
            CallDirection.Inbound => CallTypeFilter.Inbound,
            CallDirection.Outbound => CallTypeFilter.Outbound,
            CallDirection.Internal => CallTypeFilter.Internal,
            _ => CallTypeFilter.Inbound
        };
    }

    private static HandledByFilter ResolveHandledBy(Core.Entities.Call call)
    {
        if (!string.IsNullOrWhiteSpace(call.AgentId))
            return HandledByFilter.Agent;

        if (!string.IsNullOrWhiteSpace(call.Inputs))
            return HandledByFilter.Ivr;

        if (!string.IsNullOrWhiteSpace(call.Summary) || call.AutoTagIds.Count > 0)
            return HandledByFilter.Ai;

        return HandledByFilter.Any;
    }

    private static string? ResolveAbandonmentReason(Core.Entities.Call call)
    {
        if (call.Status is CallStatus.Missed or CallStatus.NoAnswer or CallStatus.Rejected)
            return call.HangupCause;

        return null;
    }

    private static string ToStatusString(CallStatus status) =>
        status switch
        {
            CallStatus.NoAnswer => "noAnswer",
            _ => char.ToLowerInvariant(status.ToString()[0]) + status.ToString()[1..]
        };

    private static IReadOnlyList<string>? NullIfEmpty(IReadOnlyList<string>? values) =>
        values is null || values.Count == 0 ? null : values;

    private static string? NullIfWhiteSpace(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
