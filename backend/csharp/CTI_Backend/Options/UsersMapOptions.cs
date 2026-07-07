namespace CtiBackend.Options;

public sealed class UsersMapOptions
{
    public Dictionary<string, int> StateCapacity { get; set; } = new()
    {
        ["ivr"] = 20, ["ai"] = 8, ["agent"] = 12, ["queue"] = 15, ["vm"] = 6, ["survey"] = 6,
    };
}
