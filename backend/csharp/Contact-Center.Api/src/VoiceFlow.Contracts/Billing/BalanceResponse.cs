using VoiceFlow.Core.ValueObjects;

namespace VoiceFlow.Contracts.Billing;

public sealed class BalanceResponse
{
    public decimal AvailableBalance { get; init; }
    public decimal UninvoicedAmount { get; init; }
    public decimal RechargeTo { get; init; }
    public decimal RechargeThreshold { get; init; }
    public bool UsageAlertsEnabled { get; init; }
    public decimal? UninvoicedLimit { get; init; }
    public string Currency { get; init; } = "USD";
    public DateTime PeriodStart { get; init; }
    public DateTime PeriodEnd { get; init; }
    public DateTime LastUpdatedAt { get; init; }
    public List<UnbilledBreakdownItem> UnbilledBreakdown { get; init; } = [];
}
