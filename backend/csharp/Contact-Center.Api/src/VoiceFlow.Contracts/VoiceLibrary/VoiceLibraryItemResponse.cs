namespace VoiceFlow.Contracts.VoiceLibrary;

public sealed class VoiceLibraryItemResponse
{
    public string Id { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string Source { get; init; } = string.Empty;
    public string? Text { get; init; }
    public string? Url { get; init; }
    public string Language { get; init; } = string.Empty;
    public string Voice { get; init; } = string.Empty;
    public bool Interruptible { get; init; }
    public int? Duration { get; init; }
    public string? FilePath { get; set; }
    public DateTime CreatedAt { get; init; }
}
