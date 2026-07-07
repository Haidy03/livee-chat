namespace CTI.Options;

public sealed class CallTTlOptions
{
    public const string SectionName = "CallTTlOptions";
    public string KeyPrefix { get; set; } = "vf:um";
    public TimeSpan CallTtl { get; set; } = TimeSpan.FromHours(6);
}
