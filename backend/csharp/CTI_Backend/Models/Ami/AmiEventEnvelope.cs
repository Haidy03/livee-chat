namespace CtiBackend.Models.Ami;

/// <summary>
/// Parsed AMI event. Known fields are lifted to properties; the full
/// key/value set is preserved in <see cref="Raw"/> so nothing is lost.
/// </summary>
public sealed class AmiEventEnvelope
{
    public string? Event { get; set; }
    public string? UserEvent { get; set; }
    public string? Channel { get; set; }
    public string? UniqueId { get; set; }
    public string? LinkedId { get; set; }
    public string? CallerIdNum { get; set; }
    public string? ConnectedLineNum { get; set; }
    public string? Context { get; set; }
    public string? Exten { get; set; }
    public string? Priority { get; set; }
    public DateTime ReceivedAtUtc { get; set; } = DateTime.UtcNow;
    public Dictionary<string, string> Raw { get; set; } = new(StringComparer.OrdinalIgnoreCase);
}
