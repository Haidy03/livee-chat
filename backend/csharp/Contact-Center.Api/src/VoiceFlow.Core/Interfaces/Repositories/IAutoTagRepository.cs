using VoiceFlow.Core.Entities;

namespace VoiceFlow.Core.Interfaces.Repositories;

public interface IAutoTagRepository : IRepository<AutoTag>
{
    Task<IEnumerable<AutoTag>> GetByTenantAsync(string tenantId, CancellationToken cancellationToken = default);
}
