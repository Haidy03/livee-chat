using VoiceFlow.Core.Entities;

namespace VoiceFlow.Core.Interfaces.Repositories;

public interface ISoftphoneCallLogRepository : IRepository<SoftphoneCallLog>
{
    Task<(IEnumerable<SoftphoneCallLog> Items, long TotalCount)> GetByUserAndTenantAsync(string userId, string tenantId, int skip, int take, CancellationToken cancellationToken = default);
}
