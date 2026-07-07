using VoiceFlow.Core.Entities;

namespace VoiceFlow.Core.Interfaces.Repositories;

public interface IEditLogRepository : IRepository<EditLog>
{
    Task<(IEnumerable<EditLog> Items, long TotalCount)> SearchAsync(
        string tenantId,
        string? entityType,
        string? entityId,
        string? userId,
        string? action,
        DateTime? from,
        DateTime? to,
        string? summarySearch,
        int skip,
        int take,
        CancellationToken cancellationToken = default);
}
