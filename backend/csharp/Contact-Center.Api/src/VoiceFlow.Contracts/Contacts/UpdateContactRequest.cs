namespace VoiceFlow.Contracts.Contacts;

public sealed class UpdateContactRequest
{
    public string? Name { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? Company { get; set; }
    public List<string>? TagIds { get; set; }
    public string? Notes { get; set; }
}
