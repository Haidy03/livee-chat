using VoiceFlow.Core.Enums;

namespace VoiceFlow.Contracts.Groups;

public sealed class UpdateGroupRequest
{
    public string? Name { get; set; }
    public RingStrategy? RingStrategy { get; set; }
    public int? RingTimeout { get; set; }
    public List<string>? Members { get; set; }
}
