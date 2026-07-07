using MongoDB.Driver;
using VoiceFlow.Core.Entities;
using VoiceFlow.Core.Interfaces.Repositories;

namespace VoiceFlow.Infrastructure.Persistence.Repositories;

public sealed class EmailAgentSettingsRepository : MongoRepository<EmailAgentSettings>, IEmailAgentSettingsRepository
{
    public EmailAgentSettingsRepository(MongoDbContext context) : base(context, "email_agent_settings") { }

    public async Task<EmailAgentSettings?> GetByAgentAsync(
        string tenantId, string agentId, CancellationToken cancellationToken = default)
    {
        var filter = Builders<EmailAgentSettings>.Filter.And(
            Builders<EmailAgentSettings>.Filter.Eq(s => s.TenantId, tenantId),
            Builders<EmailAgentSettings>.Filter.Eq(s => s.AgentId, agentId));

        return await Collection.Find(filter).FirstOrDefaultAsync(cancellationToken);
    }

    public async Task UpsertByAgentAsync(
        string tenantId, string agentId, string signatureHtml, CancellationToken cancellationToken = default)
    {
        var filter = Builders<EmailAgentSettings>.Filter.And(
            Builders<EmailAgentSettings>.Filter.Eq(s => s.TenantId, tenantId),
            Builders<EmailAgentSettings>.Filter.Eq(s => s.AgentId, agentId));

        var update = Builders<EmailAgentSettings>.Update
            .Set(s => s.SignatureHtml, signatureHtml)
            .Set(s => s.UpdatedAt, DateTime.UtcNow)
            .SetOnInsert(s => s.TenantId, tenantId)
            .SetOnInsert(s => s.AgentId, agentId)
            .SetOnInsert(s => s.CreatedAt, DateTime.UtcNow);

        await Collection.UpdateOneAsync(filter, update, new UpdateOptions { IsUpsert = true }, cancellationToken);
    }
}
