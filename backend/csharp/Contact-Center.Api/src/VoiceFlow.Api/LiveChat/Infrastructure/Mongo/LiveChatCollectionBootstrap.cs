using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using VoiceFlow.Api.LiveChat.Domain;
using VoiceFlow.Api.LiveChat.Infrastructure.Logging;
using VoiceFlow.Infrastructure.Options;

namespace VoiceFlow.Api.LiveChat.Infrastructure.Mongo;

public sealed class LiveChatCollectionBootstrap
{
    private readonly LiveChatMongoContext _context;
    private readonly MongoDbSettings _settings;
    private readonly ILogger<LiveChatCollectionBootstrap> _logger;

    public LiveChatCollectionBootstrap(
        LiveChatMongoContext context,
        IOptions<MongoDbSettings> settings,
        ILogger<LiveChatCollectionBootstrap> logger)
    {
        _context = context;
        _settings = settings.Value;
        _logger = logger;
    }

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "LiveChat Mongo bootstrap starting database={DatabaseName}",
            _settings.LiveChatDatabaseName);

        await CreateClientRequestIndexesAsync(cancellationToken);
        await CreateRoomIndexesAsync(cancellationToken);
        await CreateAgentIndexesAsync(cancellationToken);
        await CreateAgentHubLogIndexesAsync(cancellationToken);
        await CreateCannedResponseIndexesAsync(cancellationToken);
        await CreateAiSuggestionIndexesAsync(cancellationToken);

        _logger.LogInformation(
            "LiveChat Mongo bootstrap completed database={DatabaseName}",
            _settings.LiveChatDatabaseName);
    }

    private async Task CreateClientRequestIndexesAsync(CancellationToken cancellationToken)
    {
        var indexes = new[]
        {
            new CreateIndexModel<ClientRequest>(
                Builders<ClientRequest>.IndexKeys.Ascending(x => x.locked).Ascending(x => x.status.state)),
            new CreateIndexModel<ClientRequest>(
                Builders<ClientRequest>.IndexKeys.Ascending(x => x.status.state).Ascending(x => x.status.timeStamp)),
            new CreateIndexModel<ClientRequest>(
                Builders<ClientRequest>.IndexKeys.Ascending(x => x.connectionId)),
            new CreateIndexModel<ClientRequest>(
                Builders<ClientRequest>.IndexKeys.Ascending(x => x.department.id).Ascending(x => x.lang)),
        };

        await _context.ClientRequests.Indexes.CreateManyAsync(indexes, cancellationToken);
    }

    private async Task CreateRoomIndexesAsync(CancellationToken cancellationToken)
    {
        var indexes = new[]
        {
            new CreateIndexModel<Room>(
                Builders<Room>.IndexKeys.Ascending(x => x.agentId).Ascending(x => x.roomStatus)),
            new CreateIndexModel<Room>(
                Builders<Room>.IndexKeys.Ascending(x => x.channel).Ascending(x => x.contactId).Ascending(x => x.roomStatus)),
        };

        await _context.Rooms.Indexes.CreateManyAsync(indexes, cancellationToken);
    }

    private async Task CreateAgentIndexesAsync(CancellationToken cancellationToken)
    {
        var indexes = new[]
        {
            new CreateIndexModel<Agent>(
                Builders<Agent>.IndexKeys.Ascending(x => x.DepartmentIds)),
            new CreateIndexModel<Agent>(
                Builders<Agent>.IndexKeys.Ascending(x => x.Languages)),
        };

        await _context.Agents.Indexes.CreateManyAsync(indexes, cancellationToken);
    }

    private async Task CreateAgentHubLogIndexesAsync(CancellationToken cancellationToken)
    {
        var indexes = new[]
        {
            new CreateIndexModel<AgentHubLogEntry>(
                Builders<AgentHubLogEntry>.IndexKeys.Descending(x => x.Timestamp)),
            new CreateIndexModel<AgentHubLogEntry>(
                Builders<AgentHubLogEntry>.IndexKeys.Ascending(x => x.Level).Descending(x => x.Timestamp)),
        };

        await _context.AgentHubLogs.Indexes.CreateManyAsync(indexes, cancellationToken);
    }

    private async Task CreateCannedResponseIndexesAsync(CancellationToken cancellationToken)
    {
        var indexes = new[]
        {
            new CreateIndexModel<CannedResponse>(
                Builders<CannedResponse>.IndexKeys.Ascending(x => x.projectId)),
        };

        await _context.CannedResponses.Indexes.CreateManyAsync(indexes, cancellationToken);
    }

    private async Task CreateAiSuggestionIndexesAsync(CancellationToken cancellationToken)
    {
        var indexes = new[]
        {
            new CreateIndexModel<AiSuggestion>(
                Builders<AiSuggestion>.IndexKeys.Ascending(x => x.projectId).Ascending(x => x.roomId).Descending(x => x.createdAtUtc)),
            new CreateIndexModel<AiSuggestion>(
                Builders<AiSuggestion>.IndexKeys.Ascending(x => x.projectId).Ascending(x => x.requestedByAgentId).Descending(x => x.createdAtUtc)),
        };
        await _context.AiSuggestions.Indexes.CreateManyAsync(indexes, cancellationToken);
    }

}