using MongoDB.Bson;
using MongoDB.Driver;
using VoiceFlow.Core.Entities.WrapUpCodes;
using VoiceFlow.Core.Interfaces.Repositories.WrapUpCodes;

namespace VoiceFlow.Infrastructure.Persistence.Repositories.WrapUpCodes;

public sealed class QueueWrapUpCodeRepository : MongoRepository<QueueWrapUpCode>, IQueueWrapUpCodeRepository
{
    public QueueWrapUpCodeRepository(MongoDbContext context) : base(context, "queue_wrapup_codes") { }

    public async Task<IReadOnlyList<string>> ListCodeIdsAsync(string tenantId, string queueId, CancellationToken ct)
    {
        var rows = await Collection
            .Find(x => x.TenantId == tenantId && x.QueueId == queueId)
            .ToListAsync(ct);
        return rows.Select(r => r.WrapUpCodeId).ToList();
    }

    public async Task ReplaceForQueueAsync(string tenantId, string queueId, IReadOnlyList<string> codeIds, CancellationToken ct)
    {
        await Collection.DeleteManyAsync(
            x => x.TenantId == tenantId && x.QueueId == queueId, ct);

        if (codeIds.Count == 0) return;

        var now = DateTime.UtcNow;
        var docs = codeIds.Select(cid => new QueueWrapUpCode
        {
            Id = ObjectId.GenerateNewId().ToString(),
            TenantId = tenantId,
            QueueId = queueId,
            WrapUpCodeId = cid,
            CreatedAt = now,
            UpdatedAt = now,
        }).ToList();

        await Collection.InsertManyAsync(docs, cancellationToken: ct);
    }

    public Task DeleteByCodeIdAsync(string tenantId, string codeId, CancellationToken ct) =>
        Collection.DeleteManyAsync(x => x.TenantId == tenantId && x.WrapUpCodeId == codeId, ct);
}
