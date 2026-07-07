using Microsoft.Extensions.Options;
using MongoDB.Driver;
using VoiceFlow.Api.LiveChat.Domain;
using VoiceFlow.Api.LiveChat.Infrastructure.Logging;
using VoiceFlow.Infrastructure.Options;

namespace VoiceFlow.Api.LiveChat.Infrastructure.Mongo;

public sealed class LiveChatMongoContext
{
    public IMongoDatabase Database { get; }
    public IMongoClient Client { get; }
    public IMongoCollection<ClientRequest> ClientRequests { get; }
    public IMongoCollection<Room> Rooms { get; }
    public IMongoCollection<Agent> Agents { get; }
    public IMongoCollection<AgentHubLogEntry> AgentHubLogs { get; }
    public IMongoCollection<CannedResponse> CannedResponses { get; }
    public IMongoCollection<AiSuggestion> AiSuggestions { get; }

    public LiveChatMongoContext(IMongoClient client, IOptions<MongoDbSettings> mongoOptions)
    {
        Client = client;
        Database = client.GetDatabase(mongoOptions.Value.LiveChatDatabaseName);
        ClientRequests = Database.GetCollection<ClientRequest>("ClientRequest");
        Rooms = Database.GetCollection<Room>("Room");
        Agents = Database.GetCollection<Agent>("Agent");
        AgentHubLogs = Database.GetCollection<AgentHubLogEntry>("AgentHubLog");
        CannedResponses = Database.GetCollection<CannedResponse>("CannedResponse");
        AiSuggestions = Database.GetCollection<AiSuggestion>("AiSuggestion");
    }
}
