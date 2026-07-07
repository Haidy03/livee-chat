using VoiceFlow.Contracts.Accounts;
using VoiceFlow.Core.Common;

namespace VoiceFlow.Application.Interfaces;

public interface IAccountService
{
    Task<Result<AccountResponse>> GetAccountAsync(string tenantId, CancellationToken cancellationToken = default);
    Task<Result<AccountResponse>> UpdateAccountAsync(string tenantId, UpdateAccountRequest request, CancellationToken cancellationToken = default);
}
