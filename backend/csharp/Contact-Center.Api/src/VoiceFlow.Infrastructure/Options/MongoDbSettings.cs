namespace VoiceFlow.Infrastructure.Options;

public sealed class MongoDbSettings
{
    public const string SectionName = "MongoDB";

    public string ConnectionString { get; set; } = "mongodb://localhost:27017";
    public string DatabaseName { get; set; } = "voiceflow";
    public string VoiceDatabaseName { get; set; } = "voicebot";
    public string LiveChatDatabaseName { get; set; } = "Livechat-cc";
}
