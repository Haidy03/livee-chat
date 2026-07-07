using MongoDB.Driver;
using VoiceFlow.Api.LiveChat.Application.Abstractions;
using VoiceFlow.Api.LiveChat.Domain;

namespace VoiceFlow.Api.LiveChat.Infrastructure.Mongo;

public sealed class ClientRequestRepository : IClientRequestRepository
{
    private readonly LiveChatMongoContext _ctx;
    public ClientRequestRepository(LiveChatMongoContext ctx) => _ctx = ctx;

    public Task InsertAsync(ClientRequest req, CancellationToken ct = default) =>
        _ctx.ClientRequests.InsertOneAsync(req, cancellationToken: ct);

    public async Task<ClientRequest?> GetAsync(string id, CancellationToken ct = default) =>
        await _ctx.ClientRequests.Find(x => x._id == id).FirstOrDefaultAsync(ct);

    public async Task<ClientRequest?> GetAsync(string id, IClientSessionHandle session, CancellationToken ct = default) =>
        await _ctx.ClientRequests.Find(session, Builders<ClientRequest>.Filter.Eq(x => x._id, id)).FirstOrDefaultAsync(ct);

    public async Task<ClientRequest?> TryLockAsync(string id, CancellationToken ct = default)
    {
        var filter = Builders<ClientRequest>.Filter.Where(x => x._id == id && x.locked == false);
        var update = Builders<ClientRequest>.Update
            .Set(x => x.locked, true)
            .Inc(x => x.requestCount, 1)
            .Set(x => x.upadtedAt, DateTime.UtcNow);
        var opts = new FindOneAndUpdateOptions<ClientRequest> { ReturnDocument = ReturnDocument.After };
        return await _ctx.ClientRequests.FindOneAndUpdateAsync(filter, update, opts, ct);
    }

    public async Task UnlockAsync(string id, string excludedAgentId, CancellationToken ct = default)
    {
        var current = await GetAsync(id, ct);
        if (current is null) return;
        var excluded = (current.execludedAgentId ?? string.Empty)
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        if (!string.IsNullOrWhiteSpace(excludedAgentId)) excluded.Add(excludedAgentId);
        var merged = string.Join(",", excluded);

        var update = Builders<ClientRequest>.Update
            .Set(x => x.locked, false)
            .Set(x => x.execludedAgentId, merged)
            .Set(x => x.upadtedAt, DateTime.UtcNow);
        await _ctx.ClientRequests.UpdateOneAsync(x => x._id == id, update, cancellationToken: ct);
    }

    public Task SetOfflineAsync(string id, CancellationToken ct = default)
    {
        var update = Builders<ClientRequest>.Update
            .Set(x => x.status, new ConnectionStatus { state = "offline", timeStamp = DateTime.UtcNow })
            .Set(x => x.upadtedAt, DateTime.UtcNow);
        return _ctx.ClientRequests.UpdateOneAsync(x => x._id == id, update, cancellationToken: ct);
    }

    public async Task<long> DeleteAsync(string id, IClientSessionHandle? session = null, CancellationToken ct = default)
    {
        var filter = Builders<ClientRequest>.Filter.Eq(x => x._id, id);
        var res = session is null
            ? await _ctx.ClientRequests.DeleteOneAsync(filter, ct)
            : await _ctx.ClientRequests.DeleteOneAsync(session, filter, cancellationToken: ct);
        return res.DeletedCount;
    }

    public async Task<List<ClientRequest>> GetUnlockedPendingAsync(CancellationToken ct = default) =>
        await _ctx.ClientRequests
            .Find(x => x.locked == false && x.status.state == "online")
            .ToListAsync(ct);

    public async Task<List<ClientRequest>> GetStaleOfflineAsync(DateTime olderThan, CancellationToken ct = default) =>
        await _ctx.ClientRequests
            .Find(x => x.locked == false && x.status.state == "offline" && x.status.timeStamp < olderThan)
            .ToListAsync(ct);
}
