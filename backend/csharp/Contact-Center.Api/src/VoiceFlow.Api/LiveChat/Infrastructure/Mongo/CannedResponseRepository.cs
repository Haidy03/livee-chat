using MongoDB.Bson;
using MongoDB.Driver;
using VoiceFlow.Api.LiveChat.Application.Abstractions;
using VoiceFlow.Api.LiveChat.Domain;

namespace VoiceFlow.Api.LiveChat.Infrastructure.Mongo;

public sealed class CannedResponseRepository : ICannedResponseRepository
{
    private readonly LiveChatMongoContext _ctx;

    public CannedResponseRepository(LiveChatMongoContext ctx) => _ctx = ctx;

    private static FilterDefinition<CannedResponse> ByProjectAndId(string projectId, string id)
    {
        var fb = Builders<CannedResponse>.Filter;
        return fb.And(fb.Eq(x => x.projectId, projectId), fb.Eq(x => x._id, id));
    }

    public async Task<IReadOnlyList<CannedResponse>> GetAllByProjectAsync(string projectId, CancellationToken ct = default)
    {
        var filter = Builders<CannedResponse>.Filter.Eq(x => x.projectId, projectId);
        return await _ctx.CannedResponses.Find(filter).SortBy(x => x.title).ToListAsync(ct);
    }

    public async Task<CannedResponse?> GetByIdAsync(string projectId, string id, CancellationToken ct = default)
    {
        return await _ctx.CannedResponses.Find(ByProjectAndId(projectId, id)).FirstOrDefaultAsync(ct);
    }

    public async Task CreateAsync(CannedResponse entity, CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(entity._id))
            entity._id = ObjectId.GenerateNewId().ToString();
        await _ctx.CannedResponses.InsertOneAsync(entity, cancellationToken: ct);
    }

    public async Task<CannedResponse?> UpdateAsync(string projectId, string id, CannedResponse update, CancellationToken ct = default)
    {
        var u = Builders<CannedResponse>.Update
            .Set(x => x.title, update.title)
            .Set(x => x.messages, update.messages)
            .Set(x => x.updatedAt, DateTime.UtcNow);

        var opts = new FindOneAndUpdateOptions<CannedResponse> { ReturnDocument = ReturnDocument.After };
        return await _ctx.CannedResponses.FindOneAndUpdateAsync(ByProjectAndId(projectId, id), u, opts, ct);
    }

    public async Task<bool> DeleteAsync(string projectId, string id, CancellationToken ct = default)
    {
        var res = await _ctx.CannedResponses.DeleteOneAsync(ByProjectAndId(projectId, id), ct);
        return res.DeletedCount > 0;
    }
}
