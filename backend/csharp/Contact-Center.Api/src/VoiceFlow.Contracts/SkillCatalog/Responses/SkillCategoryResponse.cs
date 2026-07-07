namespace VoiceFlow.Api.SkillCatalog.Responses;

public sealed class SkillCategoryResponse
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public int SortOrder { get; set; }
    public bool Active { get; set; }
    public List<SkillOptionResponse> Options { get; set; } = new();
}

public sealed class SkillOptionResponse
{
    public string Id { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public int SortOrder { get; set; }
    public bool Active { get; set; }
    public int UsageCount { get; set; }
}
