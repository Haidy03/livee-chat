using MongoDB.Bson;
using MongoDB.Driver;
using VoiceFlow.Api.LiveChat.Application.Abstractions;
using VoiceFlow.Api.LiveChat.Domain;

namespace VoiceFlow.Api.LiveChat.Infrastructure.Mongo;

public sealed class AiSuggestionRepository : IAiSuggestionRepository
{
    private readonly LiveChatMongoContext _ctx;

    public AiSuggestionRepository(LiveChatMongoContext ctx) => _ctx = ctx;

    private static FilterDefinition<AiSuggestion> ByProjectAndId(string projectId, string id)
    {
        var fb = Builders<AiSuggestion>.Filter;
        return fb.And(fb.Eq(x => x.projectId, projectId), fb.Eq(x => x._id, id));
    }

    public async Task CreateAsync(AiSuggestion s, CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(s._id))
            s._id = ObjectId.GenerateNewId().ToString();
        await _ctx.AiSuggestions.InsertOneAsync(s, cancellationToken: ct);
    }

    public async Task<AiSuggestion?> GetByIdAsync(string projectId, string id, CancellationToken ct = default)
    {
        return await _ctx.AiSuggestions.Find(ByProjectAndId(projectId, id)).FirstOrDefaultAsync(ct);
    }

    public async Task<IReadOnlyList<AiSuggestion>> ListByRoomAsync(string projectId, string roomId, int limit, CancellationToken ct = default)
    {
        var fb = Builders<AiSuggestion>.Filter;
        var filter = fb.And(fb.Eq(x => x.projectId, projectId), fb.Eq(x => x.roomId, roomId));
        return await _ctx.AiSuggestions
            .Find(filter)
            .SortByDescending(x => x.createdAtUtc)
            .Limit(limit)
            .ToListAsync(ct);
    }

    public async Task<bool> MarkUsedAsync(string projectId, string id, string agentId, string usedText, string? sentMessageId, CancellationToken ct = default)
    {
        var fb = Builders<AiSuggestion>.Filter;
        var filter = fb.And(
            fb.Eq(x => x.projectId, projectId),
            fb.Eq(x => x._id, id),
            fb.Eq(x => x.requestedByAgentId, agentId));
        var u = Builders<AiSuggestion>.Update
            .Set(x => x.usedAtUtc, DateTime.UtcNow)
            .Set(x => x.usedSuggestionText, usedText)
            .Set(x => x.sentMessageId, sentMessageId);
        var res = await _ctx.AiSuggestions.UpdateOneAsync(filter, u, cancellationToken: ct);
        return res.ModifiedCount > 0;
    }

    public async Task<bool> AddFeedbackAsync(string projectId, string id, string agentId, AiSuggestionFeedback feedback, string? comment, CancellationToken ct = default)
    {
        var fb = Builders<AiSuggestion>.Filter;
        var filter = fb.And(
            fb.Eq(x => x.projectId, projectId),
            fb.Eq(x => x._id, id),
            fb.Eq(x => x.requestedByAgentId, agentId));
        var u = Builders<AiSuggestion>.Update
            .Set(x => x.feedback, feedback)
            .Set(x => x.feedbackComment, comment)
            .Set(x => x.feedbackAtUtc, DateTime.UtcNow);
        var res = await _ctx.AiSuggestions.UpdateOneAsync(filter, u, cancellationToken: ct);
        return res.ModifiedCount > 0;
    }
}
