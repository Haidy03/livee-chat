using MongoDB.Driver;
using VoiceFlow.Core.Entities;
using VoiceFlow.Core.Interfaces.Repositories;

namespace VoiceFlow.Infrastructure.Persistence.Repositories;

public sealed class QueueRepository : MongoRepository<Queue>, IQueueRepository
{
    public QueueRepository(MongoDbContext context) : base(context, "queues") { }

    public async Task<IEnumerable<Queue>> GetByTenantAsync(string tenantId, CancellationToken cancellationToken = default)
    {
        var filter = Builders<Queue>.Filter.Eq(q => q.TenantId, tenantId);
        return await Collection.Find(filter).SortByDescending(q => q.UpdatedAt).ToListAsync(cancellationToken);
    }

    public async Task<bool> ExistsByCodeAsync(string tenantId, string code, string? excludeId, CancellationToken cancellationToken = default)
    {
        var fb = Builders<Queue>.Filter;
        var filter = fb.Eq(q => q.TenantId, tenantId) & fb.Eq(q => q.Code, code);
        if (!string.IsNullOrEmpty(excludeId))
            filter &= fb.Ne(q => q.Id, excludeId);
        return await Collection.Find(filter).AnyAsync(cancellationToken);
    }
}
