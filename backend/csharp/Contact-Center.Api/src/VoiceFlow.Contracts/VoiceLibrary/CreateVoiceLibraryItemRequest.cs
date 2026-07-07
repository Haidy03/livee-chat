using System.ComponentModel.DataAnnotations;

namespace VoiceFlow.Contracts.VoiceLibrary;

public sealed class CreateVoiceLibraryItemRequest
{
    [Required]
    public string Name { get; set; } = string.Empty;
    [Required]
    public string Source { get; set; } = "upload";
    public string? Text { get; set; }
    public string Language { get; set; } = "ar";
    public string Voice { get; set; } = "female";
    public bool Interruptible { get; set; } = true;
}
