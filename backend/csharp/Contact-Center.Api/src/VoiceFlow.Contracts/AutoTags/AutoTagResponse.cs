namespace VoiceFlow.Contracts.AutoTags;

public sealed class AutoTagResponse
{
    public string Id { get; init; } = string.Empty;
    public string Title { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public string Prompt { get; init; } = string.Empty;
    public string Color { get; init; } = string.Empty;
    public string? TagId { get; init; }
    public bool Active { get; init; }
}
