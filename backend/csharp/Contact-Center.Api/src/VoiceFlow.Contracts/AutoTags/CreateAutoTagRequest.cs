using System.ComponentModel.DataAnnotations;

namespace VoiceFlow.Contracts.AutoTags;

public sealed class CreateAutoTagRequest
{
    [Required]
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Prompt { get; set; } = string.Empty;
    public string Color { get; set; } = "#3B82F6";
    public string? TagId { get; set; }
    public bool Active { get; set; } = true;
}
