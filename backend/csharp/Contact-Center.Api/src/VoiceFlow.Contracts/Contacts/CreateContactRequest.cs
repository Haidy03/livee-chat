using System.ComponentModel.DataAnnotations;

namespace VoiceFlow.Contracts.Contacts;

public sealed class CreateContactRequest
{
    [Required]
    public string Name { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Company { get; set; } = string.Empty;
    public List<string> TagIds { get; set; } = [];
    public string Notes { get; set; } = string.Empty;
}
