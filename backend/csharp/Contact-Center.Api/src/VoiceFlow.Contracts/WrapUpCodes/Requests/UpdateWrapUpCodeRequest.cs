using System.ComponentModel.DataAnnotations;

namespace VoiceFlow.Contracts.WrapUpCodes.Requests;

public sealed class UpdateWrapUpCodeRequest
{
    
    [MaxLength(128)] public string? Label { get; set; }
    [MaxLength(128)] public string? LabelAr { get; set; }
    [MaxLength(32)] public string? Category { get; set; }
    [MaxLength(16)] public string? Color { get; set; }
    public bool? IsActive { get; set; }
    public int? SortOrder { get; set; }
}
