namespace VoiceFlow.Contracts.FreeSwitch;


public sealed class PushDialplanDocumentsRequest
{
    public List<DialplanDocumentDto> Records { get; set; } = new();
}

public sealed class DialplanDocumentDto
{
    //public string Id { get; set; } = string.Empty;
    public string? TenantId { get; set; }
    public string Domain { get; set; } = string.Empty;
    public string Context { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public bool Enabled { get; set; } = true;
    public int Priority { get; set; } = 100;
    public string? RenderMode { get; set; } = "structured";
    public List<DialplanEntryDto> Entries { get; set; } = new();
}

public sealed class DialplanEntryDto
{
    public string Name { get; set; } = string.Empty;
    public string RouteType { get; set; } = "default";
    public int Priority { get; set; } = 100;
    public DialplanMatchDto Match { get; set; } = new();
    public DialplanValidationDto? Validation { get; set; }
    public List<DialplanActionDto> Actions { get; set; } = new();
}

public sealed class DialplanMatchDto
{
    public string Field { get; set; } = "destination_number";
    public string Type { get; set; } = "regex";
    public string Pattern { get; set; } = string.Empty;
}

public sealed class DialplanValidationDto
{
    public string? Field { get; set; }
    public string? Type { get; set; }
    public string? Pattern { get; set; }
}

public sealed class DialplanActionDto
{
    public string Application { get; set; } = string.Empty;
    public string? Data { get; set; }
}
