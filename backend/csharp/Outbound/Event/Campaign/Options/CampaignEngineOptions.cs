namespace Outbound.Event.Campaign.Options;

public sealed class CampaignRetryOptions
{
    public const string SectionName = "CampaignRetry";

    public int MaxAttempts { get; set; } = 6;
    public int BaseDelaySeconds { get; set; } = 60;
    public int MaxDelaySeconds { get; set; } = 1800;
}

public sealed class CampaignRateLimitOptions
{
    public const string SectionName = "CampaignRateLimits";

    public int TrunkCps { get; set; } = 20;
    public int ProviderPerMinute { get; set; } = 600;
    public int TenantPerMinute { get; set; } = 120;
    public int CampaignPerMinute { get; set; } = 60;
}
