using VoiceFlow.Core.Helpers;

namespace VoiceFlow.Core.Models;

/// <summary>
/// In-memory / Redis representation of a live caller. Independent from the
/// transport DTOs in VoiceFlow.Reports.Contracts so the projection layer can
/// evolve without breaking the HTTP contract.
/// </summary>
public sealed class LiveCallRecord
{
    public string Id { get; set; } = string.Empty;
    public string CallId { get; set; } = string.Empty;
    public string TenantId { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;
    public string MaskedNumber { get; set; } = string.Empty;
    public string Color { get; set; } = "#647590";

    /// <summary>
    /// Per-node-kind state. One value per flow-editor NodeKind plus the synthetic
    /// <see cref="CallState.Queue"/>. Serializes as the snake_case wire label
    /// (e.g. "ivr_menu") via <see cref="CallStateJsonConverter"/>.
    /// </summary>
    public CallState State { get; set; } = CallState.CallStart;

    public string EnteredStateAt { get; set; } = string.Empty;
    public string CallStartedAt { get; set; } = string.Empty;

    public string? FlowId { get; set; }
    public string? NodeKey { get; set; }
    public string? NodeKind { get; set; }
    public string? NodeLabel { get; set; }

    public string? IvrChoice { get; set; }
    public string? Intent { get; set; }
    public string? Detail { get; set; }

    public LiveAgentRef? Agent { get; set; }
    public int? QueuePosition { get; set; }
    public int? SurveyStep { get; set; }
    public int? SurveyTotal { get; set; }

    public string Channel { get; set; } = "voice";
    public string Direction { get; set; } = "inbound";

    public List<string>? Tags { get; set; }
    public List<LiveJourneyStep> History { get; set; } = new();
}

public sealed class LiveAgentRef
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
}

public sealed class LiveJourneyStep
{
    public string At { get; set; } = string.Empty;
    public string NodeKey { get; set; } = string.Empty;
    public string? Kind { get; set; }
    public string Label { get; set; } = string.Empty;
    public string? Sub { get; set; }
}

/// <summary>Aggregated snapshot pulled atomically from the registry.</summary>
public sealed class LiveCallsSnapshot
{
    public IReadOnlyList<LiveCallRecord> Calls { get; init; } = Array.Empty<LiveCallRecord>();
    public IReadOnlyDictionary<string, int> StateCounts { get; init; } = new Dictionary<string, int>();
    public IReadOnlyDictionary<string, int> NodeCounts { get; init; } = new Dictionary<string, int>();
    /// <summary>Ordered list of callIds in queue (head = longest waiting).</summary>
    public IReadOnlyList<string> QueueOrder { get; init; } = Array.Empty<string>();
    public int LongestWaitSec { get; init; }
    public int AvgHandleSec { get; init; }
    public int SlaPercent { get; init; }
    public int SlaTargetPercent { get; init; }
    public int AbandonedToday { get; init; }
}
