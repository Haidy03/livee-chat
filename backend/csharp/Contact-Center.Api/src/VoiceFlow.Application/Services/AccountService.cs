using VoiceFlow.Application.Interfaces;
using VoiceFlow.Contracts.Accounts;
using VoiceFlow.Core.Common;
using VoiceFlow.Core.Interfaces.Repositories;

namespace VoiceFlow.Application.Services;

public sealed class AccountService : IAccountService
{
    private readonly IAccountRepository _accountRepository;

    public AccountService(IAccountRepository accountRepository)
    {
        _accountRepository = accountRepository;
    }

    public async Task<Result<AccountResponse>> GetAccountAsync(string tenantId, CancellationToken cancellationToken = default)
    {
        var account = await _accountRepository.GetByIdAsync(tenantId, cancellationToken);
        if (account is null)
            return Result.Failure<AccountResponse>(Error.NotFound("Account", tenantId));

        return MapToResponse(account);
    }

    public async Task<Result<AccountResponse>> UpdateAccountAsync(string tenantId, UpdateAccountRequest request, CancellationToken cancellationToken = default)
    {
        var account = await _accountRepository.GetByIdAsync(tenantId, cancellationToken);
        if (account is null)
            return Result.Failure<AccountResponse>(Error.NotFound("Account", tenantId));

        if (request.OrgName is not null) account.OrgName = request.OrgName;
        if (request.DefaultCountry is not null) account.DefaultCountry = request.DefaultCountry;
        if (request.NumberFormat is not null) account.NumberFormat = request.NumberFormat;
        if (request.AutoAnswer.HasValue) account.AutoAnswer = request.AutoAnswer.Value;
        if (request.AutoAnswerSecs.HasValue) account.AutoAnswerSecs = request.AutoAnswerSecs.Value;
        if (request.AutoTagging.HasValue) account.AutoTagging = request.AutoTagging.Value;
        if (request.DialerUrl is not null) account.DialerUrl = request.DialerUrl;
        if (request.DialerMethod is not null) account.DialerMethod = request.DialerMethod;
        if (request.NotifyOnAgentChanges.HasValue) account.NotifyOnAgentChanges = request.NotifyOnAgentChanges.Value;

        await _accountRepository.UpdateAsync(account, cancellationToken);
        return MapToResponse(account);
    }

    private static AccountResponse MapToResponse(Core.Entities.Account account) => new()
    {
        Id = account.Id,
        OrgName = account.OrgName,
        DefaultCountry = account.DefaultCountry,
        NumberFormat = account.NumberFormat,
        AutoAnswer = account.AutoAnswer,
        AutoAnswerSecs = account.AutoAnswerSecs,
        AutoTagging = account.AutoTagging,
        NotifyOnAgentChanges = account.NotifyOnAgentChanges,
        PhoneNumbers = account.PhoneNumbers,
        CreatedAt = account.CreatedAt,
        UpdatedAt = account.UpdatedAt
    };
}
