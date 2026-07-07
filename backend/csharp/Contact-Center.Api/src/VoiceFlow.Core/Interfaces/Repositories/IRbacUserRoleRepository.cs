using VoiceFlow.Core.Entities;

namespace VoiceFlow.Core.Interfaces.Repositories;

public interface IRbacUserRoleRepository : IRepository<RbacUserRole>
{
    Task<IEnumerable<RbacUserRole>> GetByTenantAsync(string tenantId, CancellationToken cancellationToken = default);
    Task<IEnumerable<RbacUserRole>> GetByUserAndTenantAsync(string userId, string tenantId, CancellationToken cancellationToken = default);
    Task<IEnumerable<RbacUserRole>> GetByRoleAndTenantAsync(string roleId, string tenantId, CancellationToken cancellationToken = default);
    Task DeleteByUserAndTenantAsync(string userId, string tenantId, string roleId, CancellationToken cancellationToken = default);
}
