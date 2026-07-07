using VoiceFlow.Core.Entities;

namespace VoiceFlow.Core.Interfaces.Repositories;

public interface IGroupRepository : IRepository<Group>
{
    Task<IEnumerable<Group>> GetByTenantAsync(string tenantId, CancellationToken cancellationToken = default);
}
