namespace Outbound.Infrastructure.Ami;

public sealed class AmiRawEvent
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public DateTime ReceivedAtUtc { get; set; } = DateTime.UtcNow;
    public string? Event { get; set; }
    public string? UserEvent { get; set; }
    public string? UniqueId { get; set; }
    public string? LinkedId { get; set; }
    public Dictionary<string, string> Raw { get; set; } = new(StringComparer.OrdinalIgnoreCase);
}
