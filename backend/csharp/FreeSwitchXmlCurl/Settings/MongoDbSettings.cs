namespace VoiceFlow.FreeSwitchXmlCurl.Settings;

public sealed class MongoDbSettings
{
    public string ConnectionString { get; set; } = "mongodb://localhost:27017";
    public string DatabaseName { get; set; } = "freeswitch_config";
    public string DialplanCollection { get; set; } = "dialplan_entries";
}
