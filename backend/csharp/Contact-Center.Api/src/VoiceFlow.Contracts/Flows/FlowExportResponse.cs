namespace VoiceFlow.Contracts.Flows;

public sealed class FlowExportResponse
{
    public string Format { get; init; } = string.Empty;
    public string Content { get; init; } = string.Empty;
    public string FileName { get; init; } = string.Empty;
}
