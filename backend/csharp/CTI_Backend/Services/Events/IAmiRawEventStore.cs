using CtiBackend.Models.Ami;

namespace CtiBackend.Services.Events;

public interface IAmiRawEventStore
{
    void Add(AmiEventEnvelope env);
    IReadOnlyList<AmiRawEvent> Recent(int limit);
}
