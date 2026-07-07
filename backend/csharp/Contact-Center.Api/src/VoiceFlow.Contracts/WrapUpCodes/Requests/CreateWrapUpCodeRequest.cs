using System.ComponentModel.DataAnnotations;

namespace VoiceFlow.Contracts.WrapUpCodes.Requests;

public sealed class CreateWrapUpCodeRequest
{
    [Required, MaxLength(128)]
    public string Label { get; set; } = string.Empty;

    [MaxLength(128)]
    public string? LabelAr { get; set; }

    [MaxLength(32)]
    public string? Category { get; set; }

    [MaxLength(16)]
    public string? Color { get; set; }

    public bool IsActive { get; set; } = true;
    public int SortOrder { get; set; }
}
