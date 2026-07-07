using VoiceFlow.Application.Interfaces;
using VoiceFlow.Contracts.Billing;
using VoiceFlow.Core.Common;
using VoiceFlow.Core.Entities;
using VoiceFlow.Core.Interfaces.Repositories;

namespace VoiceFlow.Application.Services;

public sealed class BillingService : IBillingService
{
    private readonly IBillingRepository _repo;

    public BillingService(IBillingRepository repo)
    {
        _repo = repo;
    }

    private async Task<Billing> GetOrCreateAsync(string tenantId, CancellationToken ct)
    {
        var existing = await _repo.GetByTenantAsync(tenantId, ct);
        if (existing is not null) return existing;

        var fresh = new Billing { TenantId = tenantId };
        return await _repo.InsertAsync(fresh, ct);
    }

    public async Task<Result<BillingResponse>> GetBillingAsync(string tenantId, CancellationToken cancellationToken = default)
    {
        var billing = await GetOrCreateAsync(tenantId, cancellationToken);
        return MapToBilling(billing);
    }

    public async Task<Result<BillingResponse>> UpdateBillingAsync(string tenantId, UpdateBillingRequest request, CancellationToken cancellationToken = default)
    {
        var billing = await GetOrCreateAsync(tenantId, cancellationToken);

        if (request.InvoiceName is not null) billing.InvoiceName = request.InvoiceName;
        if (request.BillingEmails is not null) billing.BillingEmails = request.BillingEmails;
        if (request.BillingAddress is not null) billing.BillingAddress = request.BillingAddress;
        if (request.BillingCountry is not null) billing.BillingCountry = request.BillingCountry;
        if (request.VatNumber is not null) billing.VatNumber = request.VatNumber;
        if (request.RegistrationNumber is not null) billing.RegistrationNumber = request.RegistrationNumber;
        if (request.SendInvoicesToAdmins.HasValue) billing.SendInvoicesToAdmins = request.SendInvoicesToAdmins.Value;

        billing.UpdatedAt = DateTime.UtcNow;
        await _repo.UpdateAsync(billing, cancellationToken);
        return MapToBilling(billing);
    }

    public async Task<Result<BalanceResponse>> GetBalanceAsync(string tenantId, CancellationToken cancellationToken = default)
    {
        var billing = await GetOrCreateAsync(tenantId, cancellationToken);
        return MapToBalance(billing);
    }

    public async Task<Result<BalanceResponse>> UpdateBalanceSettingsAsync(string tenantId, UpdateBalanceSettingsRequest request, CancellationToken cancellationToken = default)
    {
        var billing = await GetOrCreateAsync(tenantId, cancellationToken);

        if (request.RechargeTo.HasValue) billing.RechargeTo = request.RechargeTo.Value;
        if (request.RechargeThreshold.HasValue) billing.RechargeThreshold = request.RechargeThreshold.Value;
        if (request.UsageAlertsEnabled.HasValue) billing.UsageAlertsEnabled = request.UsageAlertsEnabled.Value;
        if (request.UninvoicedLimit.HasValue) billing.UninvoicedLimit = request.UninvoicedLimit.Value;
        billing.BalanceUpdatedAt = DateTime.UtcNow;
        billing.UpdatedAt = DateTime.UtcNow;

        await _repo.UpdateAsync(billing, cancellationToken);
        return MapToBalance(billing);
    }

    private static BillingResponse MapToBilling(Billing b) => new()
    {
        Id = b.Id,
        InvoiceName = b.InvoiceName,
        BillingEmails = b.BillingEmails,
        BillingAddress = b.BillingAddress,
        BillingCountry = b.BillingCountry,
        VatNumber = b.VatNumber,
        RegistrationNumber = b.RegistrationNumber,
        SendInvoicesToAdmins = b.SendInvoicesToAdmins,
        CreatedAt = b.CreatedAt,
        UpdatedAt = b.UpdatedAt
    };

    private static BalanceResponse MapToBalance(Billing b)
    {
        var now = DateTime.UtcNow;
        var periodStart = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        return new BalanceResponse
        {
            AvailableBalance = b.AvailableBalance,
            UninvoicedAmount = b.UninvoicedAmount,
            RechargeTo = b.RechargeTo,
            RechargeThreshold = b.RechargeThreshold,
            UsageAlertsEnabled = b.UsageAlertsEnabled,
            UninvoicedLimit = b.UninvoicedLimit,
            Currency = string.IsNullOrWhiteSpace(b.BalanceCurrency) ? "USD" : b.BalanceCurrency,
            PeriodStart = periodStart,
            PeriodEnd = now,
            LastUpdatedAt = b.BalanceUpdatedAt ?? b.UpdatedAt,
            UnbilledBreakdown = b.UnbilledBreakdown ?? new()
        };
    }
}
