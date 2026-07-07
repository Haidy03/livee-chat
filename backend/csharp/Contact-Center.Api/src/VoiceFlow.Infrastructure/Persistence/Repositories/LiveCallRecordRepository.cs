using MongoDB.Bson;
using MongoDB.Driver;
using VoiceFlow.Core.Interfaces.Repositories;
using VoiceFlow.Infrastructure.Persistence;
using VoiceFlow.Infrastructure.Persistence.Repositories;
using VoiceFlow.Reports.Core.Entities;

namespace VoiceFlow.Reports.Infrastructure.Repositories;

public sealed class LiveCallRecordRepository : MongoRepository<LiveCall>, ILiveCallRecordRepository
{
    public LiveCallRecordRepository(MongoDbContext context) : base(context, "live_calls") { }

    public async Task AddAsync(LiveCall record, CancellationToken ct)
    {
        if (string.IsNullOrEmpty(record.Id))
            record.Id = ObjectId.GenerateNewId().ToString();
        await Collection.InsertOneAsync(record, cancellationToken: ct);
    }

    public async Task<(IReadOnlyList<LiveCall> Items, long Total)> SearchAsync(
        string tenantId,
        string? search,
        DateTime? from,
        DateTime? to,
        string? direction,
        string? channel,
        string? finalState,
        int skip,
        int take,
        CancellationToken ct)
    {
        var fb = Builders<LiveCall>.Filter;
        var filter = fb.Eq(c => c.TenantId, tenantId);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var escaped = System.Text.RegularExpressions.Regex.Escape(search.Trim());
            var regex = new BsonRegularExpression(escaped, "i");
            filter &= fb.Or(
                fb.Regex(c => c.Name, regex),
                fb.Regex(c => c.MaskedNumber, regex),
                fb.Regex(c => c.CallId, regex));
        }

        if (from.HasValue)
        {
            var iso = from.Value.ToUniversalTime().ToString("O");
            filter &= fb.Gte(c => c.EndedAt, iso);
        }
        if (to.HasValue)
        {
            var iso = to.Value.ToUniversalTime().ToString("O");
            filter &= fb.Lte(c => c.EndedAt, iso);
        }
        if (!string.IsNullOrWhiteSpace(direction))
            filter &= fb.Eq(c => c.Direction, direction);
        if (!string.IsNullOrWhiteSpace(channel))
            filter &= fb.Eq(c => c.Channel, channel);
        if (!string.IsNullOrWhiteSpace(finalState))
            filter &= fb.Eq(c => c.FinalState, finalState);

        var total = await Collection.CountDocumentsAsync(filter, cancellationToken: ct);
        var items = await Collection
            .Find(filter)
            .Sort(Builders<LiveCall>.Sort.Descending(c => c.EndedAt))
            .Skip(skip)
            .Limit(take)
            .ToListAsync(ct);

        return (items, total);
    }
}
