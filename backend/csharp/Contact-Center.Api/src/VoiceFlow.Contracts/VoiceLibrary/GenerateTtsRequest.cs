using System.ComponentModel.DataAnnotations;

namespace VoiceFlow.Contracts.VoiceLibrary;

public sealed class GenerateTtsRequest
{
    [Required]
    public string Name { get; set; } = string.Empty;
    [Required]
    public string Text { get; set; } = string.Empty;
    public string Language { get; set; } = "ar";
    public string Voice { get; set; } = "female";
}
