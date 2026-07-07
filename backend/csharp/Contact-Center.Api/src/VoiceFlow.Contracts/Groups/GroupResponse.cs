using VoiceFlow.Core.Enums;

namespace VoiceFlow.Contracts.Groups;

public sealed class GroupResponse
{
    public string Id { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public List<string> Members { get; init; } = [];
    public RingStrategy RingStrategy { get; init; }
    public int RingTimeout { get; init; }
    public int ActiveCalls { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime UpdatedAt { get; init; }
}
