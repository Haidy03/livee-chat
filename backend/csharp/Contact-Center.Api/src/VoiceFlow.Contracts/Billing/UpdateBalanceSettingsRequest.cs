namespace VoiceFlow.Contracts.Billing;

public sealed class UpdateBalanceSettingsRequest
{
    public decimal? RechargeTo { get; set; }
    public decimal? RechargeThreshold { get; set; }
    public bool? UsageAlertsEnabled { get; set; }
    public decimal? UninvoicedLimit { get; set; }
}
