using VoiceFlow.Core.Entities;

namespace VoiceFlow.Core.Interfaces.Repositories;

public interface IProfileRepository : IRepository<Profile>
{
    Task<Profile?> GetByUserIdAndTenantAsync(string userId, string tenantId, CancellationToken cancellationToken = default);
    Task<Profile?> GetByUserIdAsync(string userId, CancellationToken cancellationToken = default);
    Task<IEnumerable<Profile>> GetByTenantAsync(string tenantId, CancellationToken cancellationToken = default);
}
