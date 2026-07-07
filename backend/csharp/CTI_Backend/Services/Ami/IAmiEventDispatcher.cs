using CtiBackend.Models.Ami;

namespace CtiBackend.Services.Ami;

public interface IAmiEventDispatcher
{
    /// <summary>Queue an envelope for async processing. Non-blocking.</summary>
    void Enqueue(AmiEventEnvelope env);
}
