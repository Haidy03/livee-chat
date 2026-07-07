namespace VoiceFlow.Contracts.Visitors;

public sealed class VisitorResponse
{
    public string Id { get; set; } = string.Empty;
    public string CallId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string MaskedNumber { get; set; } = string.Empty;
    public string Color { get; set; } = "#647590";
    public string Direction { get; set; } = "inbound";
    public string Channel { get; set; } = "voice";
    public string FinalState { get; set; } = "end_call";
    public string Reason { get; set; } = "completed";
    public string CallStartedAt { get; set; } = string.Empty;
    public string EndedAt { get; set; } = string.Empty;
    public int DurationSec { get; set; }
    public string? AgentId { get; set; }
    public string? AgentName { get; set; }
    public string? FlowId { get; set; }
    public string? NodeLabel { get; set; }
    public string? IvrChoice { get; set; }
    public string? Intent { get; set; }
    public List<string>? Tags { get; set; }
}
