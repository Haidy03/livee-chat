using VoiceFlow.Core.Entities;

namespace VoiceFlow.Core.Interfaces.Repositories;

public interface IQueueRepository : IRepository<Queue>
{
    Task<IEnumerable<Queue>> GetByTenantAsync(string tenantId, CancellationToken cancellationToken = default);
    Task<bool> ExistsByCodeAsync(string tenantId, string code, string? excludeId, CancellationToken cancellationToken = default);
}
