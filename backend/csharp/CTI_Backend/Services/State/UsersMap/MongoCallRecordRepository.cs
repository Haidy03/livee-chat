// ============================================================================
// MongoDB-backed implementation of ICallRecordRepository. Mirrors
// VoiceFlow.Reports.Infrastructure.Repositories.CallRecordRepository: generates
// an ObjectId when Id is empty and InsertOneAsync into the calls collection.
// ============================================================================

using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Driver;
using CtiBackend.Options;

namespace CtiBackend.Services.State.UsersMap;

public sealed class MongoCallRecordRepository : ICallRecordRepository
{
    private readonly IMongoCollection<CallRecord> _col;

    public MongoCallRecordRepository(IMongoClient client, IOptions<MongoOptions> options)
    {
        var o = options.Value;
        _col = client.GetDatabase(o.Database).GetCollection<CallRecord>(o.CallsCollection);
    }

    public async Task AddAsync(CallRecord record, CancellationToken ct)
    {
        if (string.IsNullOrEmpty(record.Id))
            record.Id = ObjectId.GenerateNewId().ToString();
        await _col.InsertOneAsync(record, cancellationToken: ct);
    }
}
