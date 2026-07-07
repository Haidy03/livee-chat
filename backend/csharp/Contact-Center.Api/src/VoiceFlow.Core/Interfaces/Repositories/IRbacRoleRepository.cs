using VoiceFlow.Core.Entities;

namespace VoiceFlow.Core.Interfaces.Repositories;

public interface IRbacRoleRepository : IRepository<RbacRole>
{
    Task<IEnumerable<RbacRole>> GetByTenantAsync(string? tenantId, CancellationToken cancellationToken = default);
}
