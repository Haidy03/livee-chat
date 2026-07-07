using VoiceFlow.Core.Enums;

namespace VoiceFlow.Contracts.Groups;

public sealed class CreateGroupRequest
{
    public string Name { get; set; } = string.Empty;
    public RingStrategy RingStrategy { get; set; }
    public int RingTimeout { get; set; }
    public List<string> Members { get; set; } = [];
}
