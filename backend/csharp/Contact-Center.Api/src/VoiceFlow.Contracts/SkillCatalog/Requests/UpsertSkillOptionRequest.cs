using System.ComponentModel.DataAnnotations;

namespace VoiceFlow.Contracts.SkillCatalog.Requests;

public sealed class UpsertSkillOptionRequest
{
    [Required, MaxLength(120)]
    public string Label { get; set; } = string.Empty;

    public int SortOrder { get; set; }
    public bool Active { get; set; } = true;
}
