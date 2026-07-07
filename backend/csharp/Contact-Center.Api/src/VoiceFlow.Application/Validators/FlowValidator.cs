using VoiceFlow.Contracts.Flows;
using VoiceFlow.Core.Entities;

namespace VoiceFlow.Application.Validators;

public sealed class FlowValidator
{
    public FlowValidationResponse Validate(Flow flow)
    {
        var errors = new List<string>();
        var warnings = new List<string>();

        if (!flow.Nodes.Any())
            errors.Add("Flow must have at least one node.");

        var nodeIds = flow.Nodes.Select(n => n.Id).ToHashSet();

        foreach (var edge in flow.Edges)
        {
            if (!nodeIds.Contains(edge.Source))
                errors.Add($"Edge '{edge.Id}' references unknown source node '{edge.Source}'.");
            if (!nodeIds.Contains(edge.Target))
                errors.Add($"Edge '{edge.Id}' references unknown target node '{edge.Target}'.");
        }

        if (flow.Nodes.Any() && !flow.Edges.Any() && flow.Nodes.Count > 1)
            warnings.Add("Flow has multiple nodes but no edges connecting them.");

        return new FlowValidationResponse
        {
            IsValid = errors.Count == 0,
            Errors = errors,
            Warnings = warnings
        };
    }
}
