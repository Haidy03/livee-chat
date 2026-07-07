namespace CtiBackend.Options;

public sealed class CallerInfoOptions
{
    public const string SectionName = "CallerInfo";
    public string BaseUrl { get; set; } = string.Empty;
    public int TimeoutSeconds { get; set; } = 5;
}
