using VoiceFlow.Core.Entities;

namespace VoiceFlow.Core.Interfaces.Repositories;

public interface IFlowRepository : IRepository<Flow>
{
    Task<IEnumerable<Flow>> GetByTenantAsync(string tenantId, CancellationToken cancellationToken = default);
    Task<Flow?> GetByExtensionAsync(string tenantId, string extension, CancellationToken cancellationToken = default);
}
