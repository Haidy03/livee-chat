namespace CtiBackend.Models.Cti;

public sealed class CallJourneyEvent
{
    public string EventName { get; set; } = string.Empty;
    public string? UserEventName { get; set; }
    public string? NodeId { get; set; }
    public string? NodeType { get; set; }
    public string? Digit { get; set; }
    public string? Queue { get; set; }
    public string? Agent { get; set; }
    public DateTime TimestampUtc { get; set; } = DateTime.UtcNow;
    public Dictionary<string, string> Raw { get; set; } = new(StringComparer.OrdinalIgnoreCase);
}
