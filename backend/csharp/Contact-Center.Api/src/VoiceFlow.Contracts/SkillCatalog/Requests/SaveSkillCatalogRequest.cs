using System.ComponentModel.DataAnnotations;

namespace VoiceFlow.Contracts.SkillCatalog.Requests;

public sealed class SaveSkillCatalogRequest
{
    [Required]
    public List<SkillCategoryDto> Categories { get; set; } = new();
}

public sealed class SkillCategoryDto
{
    public string? Id { get; set; }

    [Required, MaxLength(120)]
    public string Name { get; set; } = string.Empty;

    public int SortOrder { get; set; }
    public bool Active { get; set; } = true;
    public List<SkillOptionDto> Options { get; set; } = new();
}

public sealed class SkillOptionDto
{
    public string? Id { get; set; }

    [Required, MaxLength(120)]
    public string Label { get; set; } = string.Empty;

    public int SortOrder { get; set; }
    public bool Active { get; set; } = true;
    public int? UsageCount { get; set; }
}
