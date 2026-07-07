using System.Text;
using VoiceFlow.Contracts.Flows;
using VoiceFlow.Core.Entities;

namespace VoiceFlow.Application.Exporters;

public sealed class AsteriskExporter
{
    public FlowExportResponse Export(
        Flow flow,
        IEnumerable<Campaign>? campaigns = null,
        IReadOnlyDictionary<string, Profile>? agentsById = null)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"; VoiceFlow Export: {flow.Name}");
        sb.AppendLine($"; Generated: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC");
        sb.AppendLine();

        var extension = flow.AssignedExtension ?? "s";
        sb.AppendLine($"[{flow.TenantId.Substring(0, Math.Min(8, flow.TenantId.Length))}]");

        int priority = 1;
        foreach (var node in flow.Nodes)
        {
            sb.AppendLine($"exten => {extension},{priority},NoOp({node.Data.Label})");
            priority++;
        }

        sb.AppendLine($"exten => {extension},{priority},Hangup()");

        // ===== queues.conf section =====
        if (campaigns is not null)
        {
            sb.AppendLine();
            sb.AppendLine("; ===== queues.conf =====");

            foreach (var campaign in campaigns)
            {
                var queueName = $"t_{campaign.TenantId}__qc_{campaign.Id}";
                sb.AppendLine();
                sb.AppendLine($"[{queueName}]");
                sb.AppendLine("strategy = ringall");
                sb.AppendLine("timeout  = 15");

                if (campaign.AgentIds is null || campaign.AgentIds.Count == 0)
                {
                    sb.AppendLine("; no members");
                    continue;
                }

                foreach (var agentId in campaign.AgentIds)
                {
                    if (agentsById is null || !agentsById.TryGetValue(agentId, out var profile) || profile is null)
                    {
                        sb.AppendLine($"; skipped: agent {agentId} not found");
                        continue;
                    }

                    if (profile.ExtensionNumber is null)
                    {
                        var label = profile.DisplayName ?? profile.Email ?? agentId;
                        sb.AppendLine($"; skipped: no extension ({label})");
                        continue;
                    }

                    var name = profile.DisplayName
                        ?? $"{profile.FirstName} {profile.LastName}".Trim()
                        ?? profile.Email
                        ?? agentId;
                    sb.AppendLine($"member => PJSIP/{profile.ExtensionNumber}  ; {name}");
                }
            }
        }

        return new FlowExportResponse
        {
            Format = "extensions.conf",
            Content = sb.ToString(),
            FileName = $"{flow.Name.Replace(" ", "_")}.conf"
        };
    }
}
