using VoiceFlow.Core.Entities;

namespace VoiceFlow.Core.Interfaces.Repositories;

public interface ISipAccountRepository : IRepository<SipAccount>
{
    Task<SipAccount?> GetByUserAndTenantAsync(string userId, string tenantId, CancellationToken cancellationToken = default);
}
