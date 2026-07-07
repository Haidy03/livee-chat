namespace VoiceFlow.Contracts.WrapUpCodes.Responses;

public sealed class WrapUpCodeResponse
{
    public string Id { get; set; } = string.Empty;
    public string TenantId { get; set; } = string.Empty;
    
    public string Label { get; set; } = string.Empty;
    public string? LabelAr { get; set; }
    public string Category { get; set; } = "general";
    public string Color { get; set; } = "#64748b";
    public bool IsActive { get; set; }
    public int SortOrder { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
