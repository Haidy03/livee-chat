using VoiceFlow.Contracts.Common;
using VoiceFlow.Contracts.SipAccounts;
using VoiceFlow.Core.Common;

namespace VoiceFlow.Application.Interfaces;

public interface ISipAccountService
{
    Task<Result<SipAccountResponse>> GetSipAccountAsync(string userId, string tenantId, CancellationToken cancellationToken = default);
    Task<Result<SipAccountResponse>> CreateSipAccountAsync(string userId, string tenantId, CreateSipAccountRequest request, CancellationToken cancellationToken = default);
    Task<Result<SipAccountResponse>> UpdateSipAccountAsync(string userId, string tenantId, UpdateSipAccountRequest request, CancellationToken cancellationToken = default);
    Task<Result<PagedResponse<SoftphoneCallLogResponse>>> GetSoftphoneCallLogsAsync(string userId, string tenantId, PaginationRequest pagination, CancellationToken cancellationToken = default);
    Task<Result<SoftphoneCallLogResponse>> CreateSoftphoneCallLogAsync(string userId, string tenantId, CreateSoftphoneCallLogRequest request, CancellationToken cancellationToken = default);
}
