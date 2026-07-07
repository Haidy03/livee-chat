namespace CtiBackend.Models.Cti;

public sealed class CallSessionState
{
    public string SessionId { get; set; } = string.Empty;
    public string? LinkedId { get; set; }
    public string? UniqueId { get; set; }
    public string? CallId { get; set; }
    public string? TenantId { get; set; }
    public string? CallerNumber { get; set; }
    public string? CallerName { get; set; }
    public string? CallerType { get; set; }
    public bool? IsVip { get; set; }
    public string? CurrentChannel { get; set; }
    public string? CurrentState { get; set; }
    public string? CurrentContext { get; set; }
    public string? CurrentExtension { get; set; }
    public string? CurrentNodeId { get; set; }
    public string? CurrentNodeType { get; set; }
    public string? CurrentQueue { get; set; }
    public string? CurrentAgent { get; set; }
    public string? LastDigit { get; set; }
    public DateTime StartedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime LastUpdatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? EndedAtUtc { get; set; }
    public bool IsEnded { get; set; }
    public List<CallJourneyEvent> Journey { get; set; } = new();
    public Dictionary<string, string> Metadata { get; set; } = new(StringComparer.OrdinalIgnoreCase);
}
