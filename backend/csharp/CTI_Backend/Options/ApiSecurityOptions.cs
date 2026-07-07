namespace CtiBackend.Options;

public sealed class ApiSecurityOptions
{
    public const string SectionName = "ApiSecurity";
    public bool Enabled { get; set; }
    public string ApiKeyHeaderName { get; set; } = "X-API-Key";
    public string ApiKey { get; set; } = string.Empty;
}
