using System.ComponentModel.DataAnnotations;

namespace VoiceFlow.Contracts.Calls;

public sealed class TranslateSummaryRequest
{
    [Required]
    public string TargetLanguage { get; set; } = string.Empty;
}
