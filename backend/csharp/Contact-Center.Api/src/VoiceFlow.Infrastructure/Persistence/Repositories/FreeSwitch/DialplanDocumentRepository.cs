using MongoDB.Driver;
using VoiceFlow.Core.Entities.FreeSwitch;
using VoiceFlow.Core.Interfaces.Repositories.FreeSwitch;
using VoiceFlow.Infrastructure.Persistence;
using VoiceFlow.Infrastructure.Persistence.Repositories;


namespace VoiceFlow.Reports.Infrastructure.Persistence.Repositories.FreeSwitch;

public sealed class DialplanDocumentRepository : MongoRepository<DialplanDocument>, IDialplanDocumentRepository
{
    public DialplanDocumentRepository(MongoDbContext context) : base(context, "dialplan_entries") { }

    public async Task<int> UpsertManyAsync(
    IEnumerable<DialplanDocument> records,
    CancellationToken ct)
    {
        var docs = records.ToList();

        var res = await Collection.DeleteManyAsync(
            Builders<DialplanDocument>.Filter.In(d => d.TenantId, docs.Select(r => r.TenantId)),
            ct);

        if (docs.Count == 0)
            return 0;

        await Collection.InsertManyAsync(docs, cancellationToken: ct);

        return docs.Count;
    }


}
