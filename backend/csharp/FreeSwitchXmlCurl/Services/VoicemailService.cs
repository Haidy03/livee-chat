using MongoDB.Driver;
using VoiceFlow.FreeSwitchXmlCurl.Models;

namespace VoiceFlow.FreeSwitchXmlCurl.Services;

public sealed class VoicemailService : IVoicemailService
{
    private readonly IMongoCollection<VoicemailMessage> _collection;
    private readonly ILogger<VoicemailService> _log;

    public VoicemailService(IMongoCollection<VoicemailMessage> collection, ILogger<VoicemailService> log)
    {
        _collection = collection;
        _log = log;
    }

    public async Task<string> RecordAsync(VoicemailMessage message, CancellationToken ct = default)
    {
        if (message.CreatedAt == default) message.CreatedAt = DateTime.UtcNow;
        if (message.Timestamp == default) message.Timestamp = DateTime.UtcNow;

        await _collection.InsertOneAsync(message, cancellationToken: ct);
        _log.LogInformation(
            "Voicemail stored id={Id} uuid={Uuid} mailbox={Mailbox} domain={Domain} duration={Duration}",
            message.Id, message.Uuid, message.Mailbox, message.Domain, message.DurationSeconds);
        return message.Id ?? string.Empty;
    }
}
