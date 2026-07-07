using VoiceFlow.Core.Entities.FreeSwitch;

namespace VoiceFlow.Core.Interfaces.Repositories.FreeSwitch;

public interface IDialplanDocumentRepository
{
    Task<int> UpsertManyAsync(IEnumerable<DialplanDocument> records, CancellationToken ct);
}
