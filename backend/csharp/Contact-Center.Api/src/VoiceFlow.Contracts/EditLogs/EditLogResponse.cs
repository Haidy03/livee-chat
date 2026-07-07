namespace VoiceFlow.Contracts.EditLogs;

public sealed class EditLogResponse
{
    public string Id { get; init; } = string.Empty;
    public string UserId { get; init; } = string.Empty;
    public string EntityType { get; init; } = string.Empty;
    public string EntityId { get; init; } = string.Empty;
    public string Action { get; init; } = string.Empty;
    public string? Field { get; init; }
    public object? OldValue { get; init; }
    public object? NewValue { get; init; }
    public string? Summary { get; init; }
    public DateTime CreatedAt { get; init; }
}
