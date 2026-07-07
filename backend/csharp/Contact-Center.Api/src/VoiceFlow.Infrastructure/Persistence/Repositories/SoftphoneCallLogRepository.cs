using MongoDB.Driver;
using VoiceFlow.Core.Entities;
using VoiceFlow.Core.Interfaces.Repositories;
using VoiceFlow.Infrastructure.Persistence;

namespace VoiceFlow.Infrastructure.Persistence.Repositories;

public sealed class SoftphoneCallLogRepository : MongoRepository<SoftphoneCallLog>, ISoftphoneCallLogRepository
{
    public SoftphoneCallLogRepository(MongoDbContext context) : base(context, "softphone_call_logs") { }

    public async Task<(IEnumerable<SoftphoneCallLog> Items, long TotalCount)> GetByUserAndTenantAsync(
        string userId, string tenantId, int skip, int take, CancellationToken cancellationToken = default)
    {
        var filter = Builders<SoftphoneCallLog>.Filter.And(
            Builders<SoftphoneCallLog>.Filter.Eq(s => s.UserId, userId),
            Builders<SoftphoneCallLog>.Filter.Eq(s => s.TenantId, tenantId));
        var q = Collection.Find(filter).SortByDescending(s => s.StartedAt);
        var total = await q.CountDocumentsAsync(cancellationToken);
        var items = await q.Skip(skip).Limit(take).ToListAsync(cancellationToken);
        return (items, total);
    }
}
