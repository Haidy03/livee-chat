namespace VoiceFlow.Api.LiveChat.Config;

public sealed class LiveChatOptions
{
    public const string SectionName = "LiveChat";
    public int OfferTimeoutSeconds { get; set; } = 20;
    public int StaleRequestGraceMinutes { get; set; } = 5;
    public string[] AllowedOrigins { get; set; } = Array.Empty<string>();
    public SignalRSettings SignalR { get; set; } = new();
    public ChannelSettings Channels { get; set; } = new();

    public sealed class SignalRSettings
    {
        public bool UseRedisBackplane { get; set; }
    }

    public sealed class ChannelSettings
    {
        public SocialChannel WhatsApp { get; set; } = new();
        public SocialChannel Messenger { get; set; } = new();
    }

    public sealed class SocialChannel
    {
        public string BaseUrl { get; set; } = string.Empty;
        public string Token { get; set; } = string.Empty;
        public string VerifyToken { get; set; } = string.Empty;
    }
}
