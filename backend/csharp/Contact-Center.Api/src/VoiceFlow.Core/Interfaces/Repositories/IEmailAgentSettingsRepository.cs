using VoiceFlow.Core.Entities;

namespace VoiceFlow.Core.Interfaces.Repositories;

public interface IEmailAgentSettingsRepository : IRepository<EmailAgentSettings>
{
    Task<EmailAgentSettings?> GetByAgentAsync(string tenantId, string agentId, CancellationToken cancellationToken = default);

    Task UpsertByAgentAsync(string tenantId, string agentId, string signatureHtml, CancellationToken cancellationToken = default);
}
