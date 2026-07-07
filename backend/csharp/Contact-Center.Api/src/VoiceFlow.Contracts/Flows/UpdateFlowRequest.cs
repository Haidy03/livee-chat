using VoiceFlow.Core.Enums;

namespace VoiceFlow.Contracts.Flows;

public sealed class UpdateFlowRequest
{
    public string? Name { get; set; }
    public string? Description { get; set; }
    public List<FlowNodeDto>? Nodes { get; set; }
    public List<FlowEdgeDto>? Edges { get; set; }
    public FlowStatus? Status { get; set; }
    public string? AssignedExtension { get; set; }
}
