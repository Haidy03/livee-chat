namespace CtiBackend.Options;

public sealed class MongoOptions
{
    public const string SectionName = "MongoDB";
    public string ConnectionString { get; set; } = "mongodb://localhost:27017";
    public string Database { get; set; } = "voiceflow";
    public string CallsCollection { get; set; } = "calls";
    public string ContactsCollection { get; set; } = "contacts";
    public string AccountsCollection { get; set; } = "accounts";
}
