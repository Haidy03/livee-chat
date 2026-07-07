using CtiBackend.Models.Ami;
using CtiBackend.Options;
using Microsoft.Extensions.Options;

namespace CtiBackend.Services.Events;

public sealed class InMemoryAmiRawEventStore : IAmiRawEventStore
{
    private readonly LinkedList<AmiRawEvent> _events = new();
    private readonly object _lock = new();
    private readonly int _max;

    public InMemoryAmiRawEventStore(IOptions<RawEventStoreOptions> options)
    {
        _max = Math.Max(100, options.Value.MaxEvents);
    }

    public void Add(AmiEventEnvelope env)
    {
        var record = new AmiRawEvent
        {
            ReceivedAtUtc = env.ReceivedAtUtc,
            Event = env.Event,
            UserEvent = env.UserEvent,
            UniqueId = env.UniqueId,
            LinkedId = env.LinkedId,
            Raw = new Dictionary<string, string>(env.Raw, StringComparer.OrdinalIgnoreCase),
        };
        lock (_lock)
        {
            _events.AddLast(record);
            while (_events.Count > _max) _events.RemoveFirst();
        }
    }

    public IReadOnlyList<AmiRawEvent> Recent(int limit)
    {
        if (limit <= 0) limit = 100;
        lock (_lock)
        {
            return _events.Reverse().Take(limit).ToList();
        }
    }
}
