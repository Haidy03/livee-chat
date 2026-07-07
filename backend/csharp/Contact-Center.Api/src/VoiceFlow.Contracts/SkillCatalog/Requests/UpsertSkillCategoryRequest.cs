using System.ComponentModel.DataAnnotations;

namespace VoiceFlow.Api.SkillCatalog.Requests;

public sealed class UpsertSkillCategoryRequest
{
    [Required, MaxLength(120)]
    public string Name { get; set; } = string.Empty;

    public int SortOrder { get; set; }
    public bool Active { get; set; } = true;
}
