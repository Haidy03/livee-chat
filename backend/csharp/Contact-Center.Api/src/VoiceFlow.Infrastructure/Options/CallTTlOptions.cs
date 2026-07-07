namespace VoiceFlow.Infrastructure.Options;

public sealed class CallTTlOptions
{
    public string KeyPrefix { get; set; } = "vf:um";
    public TimeSpan CallTtl { get; set; } = TimeSpan.FromHours(6);
}
