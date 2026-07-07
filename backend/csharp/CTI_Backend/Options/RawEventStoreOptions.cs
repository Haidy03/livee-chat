namespace CtiBackend.Options;

public sealed class RawEventStoreOptions
{
    public const string SectionName = "RawEventStore";
    public int MaxEvents { get; set; } = 5000;
}

public sealed class SessionRetentionOptions
{
    public const string SectionName = "SessionRetention";
    public int RecentEndedMinutes { get; set; } = 30;
}
