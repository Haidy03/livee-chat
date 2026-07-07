namespace VoiceFlow.Contracts.Flows;

public sealed class FlowEdgeDto
{
    public string Id { get; set; } = string.Empty;
    public string Source { get; set; } = string.Empty;
    public string SourceHandle { get; set; } = string.Empty;
    public string Target { get; set; } = string.Empty;
    public string TargetHandle { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public string Tone { get; set; } = string.Empty;
}
