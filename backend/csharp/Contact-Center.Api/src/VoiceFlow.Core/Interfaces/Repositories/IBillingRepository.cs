using VoiceFlow.Core.Entities;

namespace VoiceFlow.Core.Interfaces.Repositories;

public interface IBillingRepository : IRepository<Billing>
{
    Task<Billing?> GetByTenantAsync(string tenantId, CancellationToken cancellationToken = default);
}
