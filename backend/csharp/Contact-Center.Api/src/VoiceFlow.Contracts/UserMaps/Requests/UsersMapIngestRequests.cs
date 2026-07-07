namespace VoiceFlow.Contracts.UserMaps.Requests;

public sealed class IngestStateRequest
{
    public string TenantId { get; set; } = "default";
    public string CallId { get; set; } = string.Empty;

    public IngestNode Node { get; set; } = new();
    /// <summary>Optional override; otherwise derived from Node.Kind.</summary>
    public string? State { get; set; }

    public IngestCaller? Caller { get; set; }
    public string? IvrChoice { get; set; }
    public string? Intent { get; set; }
    public IngestAgentRef? Agent { get; set; }
    public int? QueuePosition { get; set; }
    public IngestSurvey? Survey { get; set; }
    public Dictionary<string, object>? Flags { get; set; }
    public IngestWebhook? Webhook { get; set; }
    public List<string>? Tags { get; set; }
    public string? At { get; set; }
    public string? Channel { get; set; }
    public string? Direction { get; set; }
    public string? Color { get; set; }
}

public sealed class IngestNode
{
    public string Key { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public string Kind { get; set; } = string.Empty;
    public string? FlowId { get; set; }
}

public sealed class IngestCaller
{
    public string? Name { get; set; }
    public string? Number { get; set; }
}

public sealed class IngestAgentRef
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
}

public sealed class IngestSurvey
{
    public int Step { get; set; }
    public int Total { get; set; }
}

public sealed class IngestWebhook
{
    public string? Name { get; set; }
}

public sealed class IngestEndRequest
{
    public string TenantId { get; set; } = "default";
    public string CallId { get; set; } = string.Empty;
    /// <summary>completed | abandoned | failed</summary>
    public string Reason { get; set; } = "completed";
}

public sealed class IngestMetricRequest
{
    public string TenantId { get; set; } = "default";
    public int? AvgHandleSec { get; set; }
    public int? SlaPercent { get; set; }
    public int? SlaTargetPercent { get; set; }
}
