using MongoDB.Driver;
using VoiceFlow.Api.LiveChat.Application.Abstractions;
using VoiceFlow.Api.LiveChat.Domain;

namespace VoiceFlow.Api.LiveChat.Infrastructure.Mongo;

public sealed class AgentRepository : IAgentRepository
{
    private readonly LiveChatMongoContext _ctx;
    public AgentRepository(LiveChatMongoContext ctx) => _ctx = ctx;

    public async Task<Agent?> GetAsync(string id, CancellationToken ct = default) =>
        await _ctx.Agents.Find(x => x.Id == id).FirstOrDefaultAsync(ct);
}
