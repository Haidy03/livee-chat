using VoiceFlow.Core.Enums;

namespace VoiceFlow.Contracts.Flows;

public sealed class FlowResponse
{
    public string Id { get; init; } = string.Empty;
    public string TenantId { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public FlowStatus Status { get; init; }
    public string? AssignedExtension { get; init; }
    public List<FlowNodeDto> Nodes { get; init; } = [];
    public List<FlowEdgeDto> Edges { get; init; } = [];
    public DateTime CreatedAt { get; init; }
    public DateTime UpdatedAt { get; init; }
}
