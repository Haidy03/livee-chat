namespace VoiceFlow.Contracts.Contacts;

public sealed class ContactResponse
{
    public string Id { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string Phone { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public string Company { get; init; } = string.Empty;
    public List<string> TagIds { get; init; } = [];
    public string Notes { get; init; } = string.Empty;
    public DateTime? LastCallAt { get; init; }
    public int TotalCalls { get; init; }
}
