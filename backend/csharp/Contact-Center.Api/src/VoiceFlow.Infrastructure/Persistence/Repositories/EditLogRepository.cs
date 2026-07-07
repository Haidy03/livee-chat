using MongoDB.Driver;
using VoiceFlow.Core.Entities;
using VoiceFlow.Core.Interfaces.Repositories;
using VoiceFlow.Infrastructure.Persistence;

namespace VoiceFlow.Infrastructure.Persistence.Repositories;

public sealed class EditLogRepository : MongoRepository<EditLog>, IEditLogRepository
{
    public EditLogRepository(MongoDbContext context) : base(context, "edit_logs") { }

    public async Task<(IEnumerable<EditLog> Items, long TotalCount)> SearchAsync(
        string tenantId,
        string? entityType,
        string? entityId,
        string? userId,
        string? action,
        DateTime? from,
        DateTime? to,
        string? summarySearch,
        int skip,
        int take,
        CancellationToken cancellationToken = default)
    {
        var filters = new List<FilterDefinition<EditLog>>
        {
            Builders<EditLog>.Filter.Eq(e => e.TenantId, tenantId)
        };

        if (!string.IsNullOrEmpty(entityType)) filters.Add(Builders<EditLog>.Filter.Eq(e => e.EntityType, entityType));
        if (!string.IsNullOrEmpty(entityId)) filters.Add(Builders<EditLog>.Filter.Eq(e => e.EntityId, entityId));
        if (!string.IsNullOrEmpty(userId)) filters.Add(Builders<EditLog>.Filter.Eq(e => e.UserId, userId));
        if (!string.IsNullOrEmpty(action)) filters.Add(Builders<EditLog>.Filter.Eq(e => e.Action, action));
        if (from.HasValue) filters.Add(Builders<EditLog>.Filter.Gte(e => e.CreatedAt, from.Value));
        if (to.HasValue) filters.Add(Builders<EditLog>.Filter.Lte(e => e.CreatedAt, to.Value));
        if (!string.IsNullOrWhiteSpace(summarySearch))
        {
            var escaped = System.Text.RegularExpressions.Regex.Escape(summarySearch.Trim());
            filters.Add(Builders<EditLog>.Filter.Regex(e => e.Summary!, new MongoDB.Bson.BsonRegularExpression(escaped, "i")));
        }

        var combinedFilter = Builders<EditLog>.Filter.And(filters);
        var q = Collection.Find(combinedFilter).SortByDescending(e => e.CreatedAt);
        var total = await q.CountDocumentsAsync(cancellationToken);
        var items = await q.Skip(skip).Limit(take).ToListAsync(cancellationToken);
        return (items, total);
    }
}
