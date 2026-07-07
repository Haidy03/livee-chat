using System.Text.Json;

namespace VoiceFlow.Contracts.Flows;

public sealed class FlowNodeDto
{
    public string Id { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public double X { get; set; }
    public double Y { get; set; }
    public string Label { get; set; } = string.Empty;
    public JsonElement? Config { get; set; }
}
