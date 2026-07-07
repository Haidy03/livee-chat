using System.Text.Json;

namespace VoiceFlow.Contracts.EditLogs;

public sealed class CreateEditLogRequest
{
    public string EntityType { get; set; } = string.Empty;
    public string EntityId { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public string? Field { get; set; }
    public JsonElement? OldValue { get; set; }
    public JsonElement? NewValue { get; set; }
    public string? Summary { get; set; }
}
