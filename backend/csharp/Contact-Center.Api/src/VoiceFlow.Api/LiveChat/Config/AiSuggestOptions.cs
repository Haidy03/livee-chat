namespace VoiceFlow.Api.LiveChat.Config;

public sealed class AiSuggestOptions
{
    public const string SectionName = "AiSuggest";

    public bool Enabled { get; set; } = true;
    public string Provider { get; set; } = "Mock"; // Mock | OpenAI
    public string Model { get; set; } = "gpt-4o-mini";
    public string ApiKey { get; set; } = string.Empty;
    public string BaseUrl { get; set; } = "https://api.openai.com/v1";
    public int MaxMessages { get; set; } = 30;
    public int TimeoutSeconds { get; set; } = 20;
    public int MaxSuggestionsPerMinutePerAgent { get; set; } = 10;
    public int MaxSuggestionsPerHourPerTenant { get; set; } = 100;
}
