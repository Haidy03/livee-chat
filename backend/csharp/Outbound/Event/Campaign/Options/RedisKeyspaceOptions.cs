namespace Outbound.Event.Campaign.Options;

/// <summary>
/// Mirrors CTI's <c>QueueMonitoringOptions</c> for the shared key namespace so Outbound can read
/// the same live state CTI writes ({prefix}:{env}:{tenantId}:{serverId}:queue:*).
/// </summary>
public sealed class RedisKeyspaceOptions
{
    public const string SectionName = "RedisKeyspace";

    public string RedisKeyPrefix { get; set; } = "cti";
    public string Environment { get; set; } = "prod";
    public string DefaultServerId { get; set; } = "asterisk-1";

    /// <summary>
    /// Optional hard override for the tenant segment when reading CTI's live keyspace. When empty
    /// (default) the campaign's real tenant is used, with an automatic fallback to "default" if the
    /// queue isn't found there — this covers campaign (__qc_) queues still bucketed under "default"
    /// by a CTI build predating the __qc_ parser fix. Set a value only to force a specific tenant.
    /// </summary>
    public string TenantOverride { get; set; } = "";

    /// <summary>If CTI's ami-status key indicates it's older than this, treat free-agent count as 0.</summary>
    public int AmiStaleStateSeconds { get; set; } = 60;

    /// <summary>How long the tracker caches a per-campaign free-agent count in-process.</summary>
    public int FreeAgentCacheMilliseconds { get; set; } = 750;
}

public sealed class AgentAvailabilityOptions
{
    public const string SectionName = "AgentAvailability";

    /// <summary>"redis" (default) reads CTI's live keyspace; "ami" falls back to the local AMI tracker.</summary>
    public string Source { get; set; } = "redis";
}

public sealed class ConcurrencyOptions
{
    public const string SectionName = "Concurrency";

    /// <summary>Prefix for the per-campaign in-flight counter.</summary>
    public string KeyPrefix { get; set; } = "outbound:conc:campaign:";

    /// <summary>TTL backstop — a leaked counter self-heals after this many seconds of no writes.</summary>
    public int TtlSeconds { get; set; } = 900;
}

public sealed class DispatcherOptions
{
    public const string SectionName = "Dispatcher";

    /// <summary>Heartbeat fallback tick when no events wake the dispatcher.</summary>
    public int HeartbeatMilliseconds { get; set; } = 1000;

    /// <summary>Safety cap on how many targets one campaign can dial per cycle.</summary>
    public int MaxPerCampaignPerCycle { get; set; } = 25;
}

public sealed class ReaperOptions
{
    public const string SectionName = "Reaper";

    public int SweepIntervalSeconds { get; set; } = 30;

    /// <summary>Max plausible call duration. dialingAt older than this is a candidate for reaping.</summary>
    public int MaxCallDurationSeconds { get; set; } = 3600;
}
