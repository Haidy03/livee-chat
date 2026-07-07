using MongoDB.Driver;
using VoiceFlow.Api.LiveChat.Application.Abstractions;
using VoiceFlow.Api.LiveChat.Domain;

namespace VoiceFlow.Api.LiveChat.Infrastructure.Mongo;

public sealed class RoomRepository : IRoomRepository
{
    private readonly LiveChatMongoContext _ctx;
    public RoomRepository(LiveChatMongoContext ctx) => _ctx = ctx;

    public Task InsertAsync(Room c, IClientSessionHandle? session = null, CancellationToken ct = default) =>
        session is null
            ? _ctx.Rooms.InsertOneAsync(c, cancellationToken: ct)
            : _ctx.Rooms.InsertOneAsync(session, c, cancellationToken: ct);

    public async Task<Room?> GetAsync(string id, CancellationToken ct = default) =>
        await _ctx.Rooms.Find(x => x._id == id).FirstOrDefaultAsync(ct);

    public async Task<Room?> GetActiveByChannelContactAsync(string channel, string contactId, CancellationToken ct = default) =>
        await _ctx.Rooms
            .Find(x => x.channel == channel && x.contactId == contactId && x.roomStatus == "active")
            .FirstOrDefaultAsync(ct);

    public Task AppendMessageAsync(string roomId, Message message, CancellationToken ct = default)
    {
        var update = Builders<Room>.Update.Push(x => x.Messages, message);
        return _ctx.Rooms.UpdateOneAsync(x => x._id == roomId, update, cancellationToken: ct);
    }

    public Task CloseAsync(string id, string typeOfClose, CancellationToken ct = default)
    {
        var update = Builders<Room>.Update
            .Set(x => x.roomStatus, "closed")
            .Set(x => x.updatedAt, DateTime.UtcNow)
            .Set(x => x.typeOfClose, typeOfClose);
        return _ctx.Rooms.UpdateOneAsync(x => x._id == id, update, cancellationToken: ct);
    }

    public async Task<List<Room>> GetActiveByAgentAsync(string agentId, CancellationToken ct = default) =>
        await _ctx.Rooms
            .Find(x => x.agentId == agentId && x.roomStatus == "active")
            .ToListAsync(ct);

    public Task ReassignAgentAsync(string roomId, string agentId, CancellationToken ct = default)
    {
        var update = Builders<Room>.Update.Set(x => x.agentId, agentId);
        return _ctx.Rooms.UpdateOneAsync(x => x._id == roomId, update, cancellationToken: ct);
    }

    public Task RequeueToGroupAsync(string roomId, string groupId, CancellationToken ct = default)
    {
        var update = Builders<Room>.Update
            .Set(x => x.agentId, string.Empty)
            .Set(x => x.department.id, groupId);
        return _ctx.Rooms.UpdateOneAsync(x => x._id == roomId, update, cancellationToken: ct);
    }
}
