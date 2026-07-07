using VoiceFlow.Contracts.Billing;
using VoiceFlow.Core.Common;

namespace VoiceFlow.Application.Interfaces;

public interface IBillingService
{
    Task<Result<BillingResponse>> GetBillingAsync(string tenantId, CancellationToken cancellationToken = default);
    Task<Result<BillingResponse>> UpdateBillingAsync(string tenantId, UpdateBillingRequest request, CancellationToken cancellationToken = default);
    Task<Result<BalanceResponse>> GetBalanceAsync(string tenantId, CancellationToken cancellationToken = default);
    Task<Result<BalanceResponse>> UpdateBalanceSettingsAsync(string tenantId, UpdateBalanceSettingsRequest request, CancellationToken cancellationToken = default);
}
