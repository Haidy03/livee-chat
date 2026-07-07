namespace VoiceFlow.Contracts.UserMaps.Responses;
public sealed class UsersMapSnapshotResponse
{
    public string GeneratedAt { get; set; } = string.Empty;
    public UsersMapMetricsResponse Metrics { get; set; } = new();
    public Dictionary<string, StateBucketResponse> States { get; set; } = new();
    public List<CallerResponse> Callers { get; set; } = new();
    public FlowSummaryResponse Flow { get; set; } = new();
}

public sealed class UsersMapMetricsResponse
{
    public int ActiveNow { get; set; }
    public int InQueue { get; set; }
    public int LongestWaitSec { get; set; }
    public int AvgHandleSec { get; set; }
    public int AbandonedToday { get; set; }
    public int SlaPercent { get; set; }
    public int SlaTargetPercent { get; set; }
}

public sealed class StateBucketResponse
{
    public int Count { get; set; }
    public int Capacity { get; set; }
}

public sealed class CallerResponse
{
    public string Id { get; set; } = string.Empty;
    public string CallId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string MaskedNumber { get; set; } = string.Empty;
    public string Color { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty;
    public string EnteredStateAt { get; set; } = string.Empty;
    public string CallStartedAt { get; set; } = string.Empty;
    public string? IvrChoice { get; set; }
    public string? Intent { get; set; }
    public string? Detail { get; set; }
    public AgentRefResponse? Agent { get; set; }
    public int? QueuePosition { get; set; }
    public int? SurveyStep { get; set; }
    public int? SurveyTotal { get; set; }
    public string Channel { get; set; } = "voice";
    public string Direction { get; set; } = "inbound";
    public List<string>? Tags { get; set; }
    public List<JourneyStepResponse> History { get; set; } = new();
}

public sealed class AgentRefResponse
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
}

public sealed class JourneyStepResponse
{
    public string At { get; set; } = string.Empty;
    public string NodeKey { get; set; } = string.Empty;
    public string? Kind { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public string? Sub { get; set; }
}

public sealed class FlowSummaryResponse
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public List<FlowNodeResponse> Nodes { get; set; } = new();
}

public sealed class FlowNodeResponse
{
    public string Key { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public int Count { get; set; }
}
