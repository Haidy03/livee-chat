namespace CtiBackend.Options;

public sealed class AmiOptions
{
    public const string SectionName = "Ami";
    public string Host { get; set; } = "127.0.0.1";
    public int Port { get; set; } = 5038;
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public int ReconnectDelaySeconds { get; set; } = 5;
    public bool EnableEvents { get; set; } = true;
}
