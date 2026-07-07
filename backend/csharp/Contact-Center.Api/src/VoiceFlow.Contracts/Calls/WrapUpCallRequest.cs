namespace VoiceFlow.Api.Calls;

public sealed class WrapUpCallRequest
{
    public string SipCallId { get; set; } = string.Empty;
    public WrapUpDto WrapUp { get; set; } = new();
}

public sealed class WrapUpDto
{
    public string Disposition { get; set; } = string.Empty;
    public string? Notes { get; set; }
    public bool CallbackScheduled { get; set; }
    public int AcwSeconds { get; set; }
    public DateTime CompletedAt { get; set; }
    public string AgentId { get; set; } = string.Empty;
    public string Status { get; set; } = "wrapped";
}
