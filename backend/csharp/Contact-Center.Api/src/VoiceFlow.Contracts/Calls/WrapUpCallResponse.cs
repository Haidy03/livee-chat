namespace VoiceFlow.Contracts.Calls;

public sealed class WrapUpCallResponse
{
    public string SipCallId { get; set; } = string.Empty;
    public string Status { get; set; } = "wrapped";
    public DateTime CompletedAt { get; set; }
}
