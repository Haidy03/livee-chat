using System.ComponentModel.DataAnnotations;

namespace VoiceFlow.Contracts.Flows;

public sealed class CreateFlowRequest
{
    [Required]
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public List<FlowNodeDto> Nodes { get; set; } = [];
    public List<FlowEdgeDto> Edges { get; set; } = [];
}
