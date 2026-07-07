using VoiceFlow.Core.Entities;

namespace VoiceFlow.Core.Interfaces.Repositories;

public interface IVoiceLibraryRepository : IRepository<VoiceLibraryItem>
{
    Task<IEnumerable<VoiceLibraryItem>> GetByTenantAsync(string tenantId, CancellationToken cancellationToken = default);
}
