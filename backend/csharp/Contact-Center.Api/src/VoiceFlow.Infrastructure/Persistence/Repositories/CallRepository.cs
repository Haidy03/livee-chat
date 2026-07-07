using MongoDB.Bson;
using MongoDB.Driver;
using VoiceFlow.Contracts.Calls;
using VoiceFlow.Core.Common;
using VoiceFlow.Core.Entities;
using VoiceFlow.Core.Enums;
using VoiceFlow.Core.Interfaces.Repositories;
using VoiceFlow.Core.Models;
using VoiceFlow.Infrastructure.Persistence;

namespace VoiceFlow.Infrastructure.Persistence.Repositories;

public sealed class CallRepository : MongoRepository<Call>, ICallRepository
{
    public CallRepository(MongoDbContext context) : base(context, "calls") { }

    public async Task<Call?> GetByTenantAndExternalCallIdAsync(string tenantId, string externalCallId, CancellationToken cancellationToken = default)
    {
        var filter = Builders<Call>.Filter.And(
            Builders<Call>.Filter.Eq(c => c.TenantId, tenantId),
            Builders<Call>.Filter.Eq(c => c.CallId, externalCallId));
        return await Collection.Find(filter).FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<(IEnumerable<Call> Items, long TotalCount)> SearchAsync(
        string tenantId,
        CallDirection? direction,
        CallStatus? status,
        DateTime? from,
        DateTime? to,
        string? caller,
        IEnumerable<string>? tagIds,
        string? userId,
        bool? softphoneOnly,
        int skip,
        int take,
        CancellationToken cancellationToken = default)
    {
        var filters = new List<FilterDefinition<Call>>
        {
            Builders<Call>.Filter.Eq(c => c.TenantId, tenantId)
        };

        if (direction.HasValue) filters.Add(Builders<Call>.Filter.Eq(c => c.Direction, direction.Value));
        if (status.HasValue) filters.Add(Builders<Call>.Filter.Eq(c => c.Status, status.Value));
        if (from.HasValue) filters.Add(Builders<Call>.Filter.Gte(c => c.StartedAt, from.Value));
        if (to.HasValue) filters.Add(Builders<Call>.Filter.Lte(c => c.StartedAt, to.Value));
        if (!string.IsNullOrEmpty(caller)) filters.Add(Builders<Call>.Filter.Regex(c => c.Caller, new BsonRegularExpression(caller, "i")));
        if (tagIds?.Any() == true) filters.Add(Builders<Call>.Filter.AnyIn(c => c.TagIds, tagIds));
        if (!string.IsNullOrEmpty(userId)) filters.Add(Builders<Call>.Filter.Eq(c => c.UserId, userId));
        if (softphoneOnly == true)
        {
            filters.Add(Builders<Call>.Filter.And(
                Builders<Call>.Filter.Ne(c => c.CallId, null),
                Builders<Call>.Filter.Ne(c => c.CallId, string.Empty)));
        }

        var combinedFilter = Builders<Call>.Filter.And(filters);
        var sortedQuery = Collection.Find(combinedFilter).SortByDescending(c => c.StartedAt);

        var totalCount = await sortedQuery.CountDocumentsAsync(cancellationToken);
        var items = await sortedQuery.Skip(skip).Limit(take).ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    public async Task<(IEnumerable<Call> Items, long TotalCount)> AdvancedSearchAsync(
        string tenantId,
        CallAdvancedSearchCriteria criteria,
        CancellationToken cancellationToken = default)
    {
        var filters = new List<FilterDefinition<Call>>
        {
            Builders<Call>.Filter.Eq(c => c.TenantId, tenantId)
        };

        if (criteria.DateFrom.HasValue)
            filters.Add(Builders<Call>.Filter.Gte(c => c.StartedAt, criteria.DateFrom.Value));
        if (criteria.DateTo.HasValue)
            filters.Add(Builders<Call>.Filter.Lte(c => c.StartedAt, criteria.DateTo.Value));

        if (criteria.DirectionStates?.Count > 0)
        {
            var directionStateFilters = criteria.DirectionStates
                .Select(ds => Builders<Call>.Filter.And(
                    Builders<Call>.Filter.Eq(c => c.Direction, ds.Direction),
                    Builders<Call>.Filter.In(c => c.Status, ds.Statuses)))
                .ToList();
            filters.Add(Builders<Call>.Filter.Or(directionStateFilters));
        }
        else
        {
            if (criteria.Directions?.Count > 0)
                filters.Add(Builders<Call>.Filter.In(c => c.Direction, criteria.Directions));
            if (criteria.Statuses?.Count > 0)
                filters.Add(Builders<Call>.Filter.In(c => c.Status, criteria.Statuses));
        }

        if (criteria.HasRecording == true)
            filters.Add(Builders<Call>.Filter.Eq(c => c.HasRecording, true));
        if (criteria.HasVoicemail == true)
            filters.Add(Builders<Call>.Filter.Eq(c => c.Status, CallStatus.Voicemail));
        if (criteria.HasHold == true)
        {
            filters.Add(Builders<Call>.Filter.Or(
                Builders<Call>.Filter.Gt(c => c.HoldSeconds, 0),
                Builders<Call>.Filter.Gt(c => c.TotalHoldSeconds, 0)));
        }
        if (criteria.HasTransfer == true)
        {
            filters.Add(Builders<Call>.Filter.Regex(
                c => c.HangupCause,
                new BsonRegularExpression("transfer", "i")));
        }

        if (criteria.Sentiments?.Count > 0)
        {
            filters.Add(
    Builders<Call>.Filter.In(
        "sentiment",
        criteria.Sentiments.Select(x => (int)x)
    )
);
        }

        if (criteria.HangUpByAgent)
        {
            filters.Add(Builders<Call>.Filter.Regex(
                c => c.HangupCause,
                new BsonRegularExpression("agent", "i")));
        }

        if (criteria.AbandonmentReasons?.Count > 0)
            filters.Add(Builders<Call>.Filter.In(c => c.HangupCause, criteria.AbandonmentReasons));

        if (criteria.AgentIds?.Count > 0)
            filters.Add(Builders<Call>.Filter.In(c => c.AgentId, criteria.AgentIds));

        if (criteria.GroupIds?.Count > 0)
            filters.Add(Builders<Call>.Filter.In(c => c.GroupId, criteria.GroupIds));

        if (criteria.TagIds?.Count > 0)
            filters.Add(Builders<Call>.Filter.AnyIn(c => c.TagIds, criteria.TagIds));

        if (!string.IsNullOrWhiteSpace(criteria.Caller))
            filters.Add(Builders<Call>.Filter.Regex(c => c.Caller, new BsonRegularExpression(EscapeRegex(criteria.Caller), "i")));

        if (!string.IsNullOrWhiteSpace(criteria.Callee))
            filters.Add(Builders<Call>.Filter.Regex(c => c.Called, new BsonRegularExpression(EscapeRegex(criteria.Callee), "i")));

        filters.Add(BuildHandledByFilter(criteria.HandledBy));

        if (!string.IsNullOrWhiteSpace(criteria.CallId))
        {
            filters.Add(Builders<Call>.Filter.Or(
                Builders<Call>.Filter.Eq(c => c.Id, criteria.CallId),
                Builders<Call>.Filter.Eq(c => c.CallId, criteria.CallId)));
        }

        if (!string.IsNullOrWhiteSpace(criteria.ReferenceId))
            filters.Add(Builders<Call>.Filter.Eq(c => c.CallId, criteria.ReferenceId));

        if (!string.IsNullOrWhiteSpace(criteria.Keyword))
            filters.Add(BuildKeywordFilter(criteria.Keyword, criteria.SearchOperator));

        AddDurationFilter(filters, criteria.Duration, c => c.TotalSeconds);
        AddDurationFilter(filters, criteria.HandlingDuration, c => c.ActiveSeconds);
        AddDurationFilter(filters, criteria.WaitingDuration, c => c.RingSeconds);
        AddDurationFilter(filters, criteria.HoldingDuration, c => c.TotalHoldSeconds);

        var combinedFilter = Builders<Call>.Filter.And(filters);
        var sort = BuildSort(criteria.SortBy, criteria.SortDescending);
        var query = Collection.Find(combinedFilter).Sort(sort);

        var totalCount = await query.CountDocumentsAsync(cancellationToken);
        var items = await query.Skip(criteria.Skip).Limit(criteria.Take).ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    public async Task<IReadOnlyList<string>> GetDistinctHangupCausesAsync(string tenantId, CancellationToken cancellationToken = default)
    {
        var filter = Builders<Call>.Filter.And(
            Builders<Call>.Filter.Eq(c => c.TenantId, tenantId),
            Builders<Call>.Filter.Ne(c => c.HangupCause, null),
            Builders<Call>.Filter.Ne(c => c.HangupCause, string.Empty));

        var values = await Collection.Distinct(c => c.HangupCause, filter).ToListAsync(cancellationToken);
        return values
            .Where(v => !string.IsNullOrWhiteSpace(v))
            .Select(v => v!)
            .OrderBy(v => v, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    public async Task<IReadOnlyList<Call>> GetActiveCallsForTenantAsync(string tenantId, CancellationToken cancellationToken = default)
    {
        var filter = Builders<Call>.Filter.And(
            Builders<Call>.Filter.Eq(c => c.TenantId, tenantId),
            Builders<Call>.Filter.Eq(c => c.EndedAt, null),
            Builders<Call>.Filter.Ne(c => c.CallId, null),
            Builders<Call>.Filter.Ne(c => c.CallId, string.Empty));

        return await Collection
            .Find(filter)
            .SortByDescending(c => c.StartedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> SetRecordingUrlAsync(
        string id,
        string recordingUrl,
        CallAnalysisResult? analysis = null,
        CancellationToken cancellationToken = default)
    {
        var filter = Builders<Call>.Filter.And(
            Builders<Call>.Filter.Eq(c => c.Id, id));

        var update = Builders<Call>.Update
            .Set(c => c.RecordingUrl, recordingUrl)
            .Set(c => c.HasRecording, true)
            .Set(c => c.UpdatedAt, DateTime.UtcNow);

        if (analysis is not null)
        {
            if (!string.IsNullOrWhiteSpace(analysis.Summary))
            {
                update = update.Set(c => c.Summary, analysis.Summary);
            }

            if (!string.IsNullOrWhiteSpace(analysis.Transcript))
            {
                update = update.Set(c => c.FullTranscript, analysis.Transcript);
            }

            if (analysis.Segments.Count > 0)
            {
                update = update.Set(c => c.Segments, analysis.Segments.Select(x => new TranscriptSegment
                {
                    Duration = x.Duration,
                    Offset = x.Offset,
                    Text = x.Text,
                    Speaker = x.Speaker
                }));
            }

            var sentiment = MapSentiment(analysis.Sentiment);
            if (sentiment.HasValue)
            {
                update = update.Set(c => c.Sentiment, sentiment.Value);
            }
        }

        var result = await Collection.UpdateOneAsync(
       filter,
       update,
       cancellationToken: cancellationToken);



        return result.MatchedCount > 0; ;
    }

    public async Task<int> CloseNonTerminatedCallsAsync()
    {
        var now = DateTime.UtcNow;
        var filter = Builders<Call>.Filter.And(
            Builders<Call>.Filter.Eq(c => c.EndedAt, null),
            Builders<Call>.Filter.Lte(c => c.StartedAt, now.AddMinutes(-30)),
            Builders<Call>.Filter.Eq(c => c.Status, CallStatus.InProgress)
        );

        //var data = await _voiceFlowDb.Calls.Find(filter).ToListAsync();

        var update = Builders<Call>.Update
            .Set(c => c.Status, CallStatus.Completed)
            .Set(c => c.EndedAt, DateTime.UtcNow)
            .Set(c => c.UpdatedAt, DateTime.UtcNow);

        var result = await Collection.UpdateManyAsync(
        filter,
        update);

        return (int)result.ModifiedCount;

    }

    private static FilterDefinition<Call> BuildHandledByFilter(HandledByCriteria handledBy) =>
        handledBy switch
        {
            HandledByCriteria.Agent => Builders<Call>.Filter.And(
                Builders<Call>.Filter.Ne(c => c.AgentId, null),
                Builders<Call>.Filter.Ne(c => c.AgentId, string.Empty)),
            HandledByCriteria.Ivr => Builders<Call>.Filter.And(
                Builders<Call>.Filter.Or(
                    Builders<Call>.Filter.Eq(c => c.AgentId, null),
                    Builders<Call>.Filter.Eq(c => c.AgentId, string.Empty)),
                Builders<Call>.Filter.Ne(c => c.Inputs, null),
                Builders<Call>.Filter.Ne(c => c.Inputs, string.Empty)),
            HandledByCriteria.Ai => Builders<Call>.Filter.And(
                Builders<Call>.Filter.Or(
                    Builders<Call>.Filter.Eq(c => c.AgentId, null),
                    Builders<Call>.Filter.Eq(c => c.AgentId, string.Empty)),
                Builders<Call>.Filter.Or(
                    Builders<Call>.Filter.And(
                        Builders<Call>.Filter.Ne(c => c.Summary, null),
                        Builders<Call>.Filter.Ne(c => c.Summary, string.Empty)),
                    Builders<Call>.Filter.SizeGt(c => c.AutoTagIds, 0))),
            _ => Builders<Call>.Filter.Empty
        };

    private static FilterDefinition<Call> BuildKeywordFilter(string keyword, SearchOperatorCriteria searchOperator)
    {
        if (searchOperator == SearchOperatorCriteria.Phrase)
        {
            var pattern = EscapeRegex(keyword.Trim());
            var regex = new BsonRegularExpression(pattern, "i");
            return Builders<Call>.Filter.Or(
                Builders<Call>.Filter.Regex(c => c.Notes, regex),
                Builders<Call>.Filter.Regex(c => c.Summary!, regex),
                Builders<Call>.Filter.Regex(c => c.FullTranscript!, regex),
                Builders<Call>.Filter.Regex(c => c.Caller, regex),
                Builders<Call>.Filter.Regex(c => c.Called, regex),
                Builders<Call>.Filter.Regex(c => c.Inputs, regex),
                Builders<Call>.Filter.Regex(c => c.CallId!, regex));
        }

        var terms = keyword.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (terms.Length == 0)
            return Builders<Call>.Filter.Empty;

        if (searchOperator == SearchOperatorCriteria.Or)
        {
            var termFilters = terms.Select(term =>
            {
                var regex = new BsonRegularExpression(EscapeRegex(term), "i");
                return Builders<Call>.Filter.Or(
                    Builders<Call>.Filter.Regex(c => c.Notes, regex),
                    Builders<Call>.Filter.Regex(c => c.Summary!, regex),
                    Builders<Call>.Filter.Regex(c => c.FullTranscript!, regex),
                    Builders<Call>.Filter.Regex(c => c.Caller, regex),
                    Builders<Call>.Filter.Regex(c => c.Called, regex),
                    Builders<Call>.Filter.Regex(c => c.Inputs, regex),
                    Builders<Call>.Filter.Regex(c => c.CallId!, regex));
            }).ToList();
            return Builders<Call>.Filter.Or(termFilters);
        }

        var andFilters = terms.Select(term =>
        {
            var regex = new BsonRegularExpression(EscapeRegex(term), "i");
            return Builders<Call>.Filter.Or(
                Builders<Call>.Filter.Regex(c => c.Notes, regex),
                Builders<Call>.Filter.Regex(c => c.Summary!, regex),
                Builders<Call>.Filter.Regex(c => c.FullTranscript!, regex),
                Builders<Call>.Filter.Regex(c => c.Caller, regex),
                Builders<Call>.Filter.Regex(c => c.Called, regex),
                Builders<Call>.Filter.Regex(c => c.Inputs, regex),
                Builders<Call>.Filter.Regex(c => c.CallId!, regex));
        }).ToList();
        return Builders<Call>.Filter.And(andFilters);
    }

    private static void AddDurationFilter(
        List<FilterDefinition<Call>> filters,
        DurationRangeCriteria? duration,
        System.Linq.Expressions.Expression<Func<Call, int>> field)
    {
        if (duration is null)
            return;

        filters.Add(Builders<Call>.Filter.And(
            Builders<Call>.Filter.Gte(field, duration.Min),
            Builders<Call>.Filter.Lte(field, duration.Max)));
    }

    private static SortDefinition<Call> BuildSort(string sortBy, bool descending)
    {
        var sort = sortBy.Trim().ToLowerInvariant() switch
        {
            "ringseconds" => descending
                ? Builders<Call>.Sort.Descending(c => c.RingSeconds)
                : Builders<Call>.Sort.Ascending(c => c.RingSeconds),
            "holdseconds" => descending
                ? Builders<Call>.Sort.Descending(c => c.HoldSeconds)
                : Builders<Call>.Sort.Ascending(c => c.HoldSeconds),
            "activeseconds" => descending
                ? Builders<Call>.Sort.Descending(c => c.ActiveSeconds)
                : Builders<Call>.Sort.Ascending(c => c.ActiveSeconds),
            "totalseconds" => descending
                ? Builders<Call>.Sort.Descending(c => c.TotalSeconds)
                : Builders<Call>.Sort.Ascending(c => c.TotalSeconds),
            "status" => descending
                ? Builders<Call>.Sort.Descending(c => c.Status)
                : Builders<Call>.Sort.Ascending(c => c.Status),
            "caller" => descending
                ? Builders<Call>.Sort.Descending(c => c.Caller)
                : Builders<Call>.Sort.Ascending(c => c.Caller),
            "called" => descending
                ? Builders<Call>.Sort.Descending(c => c.Called)
                : Builders<Call>.Sort.Ascending(c => c.Called),
            _ => descending
                ? Builders<Call>.Sort.Descending(c => c.StartedAt)
                : Builders<Call>.Sort.Ascending(c => c.StartedAt)
        };

        return sort;
    }

    private static Sentiment? MapSentiment(SentimentResult sentiment)
    {
        return sentiment.Overall?.ToLowerInvariant() switch
        {
            "positive" => Sentiment.Positive,
            "negative" => Sentiment.Negative,
            "neutral" => Sentiment.Neutral,
            "mixed" when sentiment.Positive >= sentiment.Negative && sentiment.Positive >= sentiment.Neutral
                => Sentiment.Positive,
            "mixed" when sentiment.Negative >= sentiment.Neutral => Sentiment.Negative,
            "mixed" => Sentiment.Neutral,
            _ => null
        };
    }

    private static string EscapeRegex(string value) =>
        value.Replace("\\", "\\\\", StringComparison.Ordinal)
            .Replace(".", "\\.", StringComparison.Ordinal)
            .Replace("*", "\\*", StringComparison.Ordinal)
            .Replace("+", "\\+", StringComparison.Ordinal)
            .Replace("?", "\\?", StringComparison.Ordinal)
            .Replace("^", "\\^", StringComparison.Ordinal)
            .Replace("$", "\\$", StringComparison.Ordinal)
            .Replace("{", "\\{", StringComparison.Ordinal)
            .Replace("}", "\\}", StringComparison.Ordinal)
            .Replace("[", "\\[", StringComparison.Ordinal)
            .Replace("]", "\\]", StringComparison.Ordinal)
            .Replace("|", "\\|", StringComparison.Ordinal)
            .Replace("(", "\\(", StringComparison.Ordinal)
            .Replace(")", "\\)", StringComparison.Ordinal);
}
