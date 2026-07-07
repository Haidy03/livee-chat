namespace VoiceFlow.Application.Options;

public sealed class UsersMapOptions
{
    /// <summary>simulator | redis. Defaults to simulator for backwards compatibility.</summary>
    public string Source { get; set; } = "simulator";

    public Dictionary<string, int> StateCapacity { get; set; } = new()
    {
        ["ivr"] = 20, ["ai"] = 8, ["agent"] = 12, ["queue"] = 15, ["vm"] = 6, ["survey"] = 6,
    };

    public string IngestToken { get; set; } = string.Empty;
}
