using VoiceFlow.Core.Entities.WrapUpCodes;

namespace VoiceFlow.Core.Interfaces.Repositories.WrapUpCodes;

public interface IWrapUpCodeRepository
{
    Task<IReadOnlyList<WrapUpCode>> ListAsync(string tenantId, bool activeOnly, CancellationToken ct);
    Task<WrapUpCode?> GetAsync(string tenantId, string id, CancellationToken ct);
    
    Task UpsertAsync(WrapUpCode entity, CancellationToken ct);
    Task<bool> DeleteAsync(string tenantId, string id, CancellationToken ct);
    Task<IReadOnlyList<WrapUpCode>> GetByIdsAsync(string tenantId, IEnumerable<string> ids, CancellationToken ct);
}

public interface IQueueWrapUpCodeRepository
{
    Task<IReadOnlyList<string>> ListCodeIdsAsync(string tenantId, string queueId, CancellationToken ct);
    Task ReplaceForQueueAsync(string tenantId, string queueId, IReadOnlyList<string> codeIds, CancellationToken ct);
    Task DeleteByCodeIdAsync(string tenantId, string codeId, CancellationToken ct);
}
