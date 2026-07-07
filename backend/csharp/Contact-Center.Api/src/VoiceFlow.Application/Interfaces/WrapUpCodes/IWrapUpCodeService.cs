using VoiceFlow.Contracts.WrapUpCodes.Requests;
using VoiceFlow.Contracts.WrapUpCodes.Responses;

namespace VoiceFlow.Application.Interfaces.WrapUpCodes;

public interface IWrapUpCodeService
{
    Task<IReadOnlyList<WrapUpCodeResponse>> ListAsync(string tenantId, bool activeOnly, CancellationToken ct);
    Task<WrapUpCodeResponse> CreateAsync(string tenantId, CreateWrapUpCodeRequest request, CancellationToken ct);
    Task<WrapUpCodeResponse> UpdateAsync(string tenantId, string id, UpdateWrapUpCodeRequest request, CancellationToken ct);
    Task<bool> DeleteAsync(string tenantId, string id, CancellationToken ct);

    Task<IReadOnlyList<string>> GetQueueCodeIdsAsync(string tenantId, string queueId, CancellationToken ct);
    Task SetQueueCodesAsync(string tenantId, string queueId, IReadOnlyList<string> codeIds, CancellationToken ct);
    Task<IReadOnlyList<WrapUpCodeResponse>> GetEffectiveForQueueAsync(string tenantId, string queueId, CancellationToken ct);
}
