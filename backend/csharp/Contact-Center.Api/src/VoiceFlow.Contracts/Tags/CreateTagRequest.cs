using System.ComponentModel.DataAnnotations;

namespace VoiceFlow.Contracts.Tags;

public sealed class CreateTagRequest
{
    [Required]
    public string Label { get; set; } = string.Empty;
    public string Color { get; set; } = "#3B82F6";
}
