namespace Outbound.Infrastructure.Ami;

/// <summary>
/// Generic seam invoked by <see cref="AmiEventDispatcher"/> for every parsed AMI event.
/// Register implementations as singletons; the dispatcher fans events out to all of them.
/// </summary>
public interface IAmiEventHandler
{
    Task HandleAsync(AmiEventEnvelope env, CancellationToken ct);
}

public interface IAmiEventDispatcher
{
    void Enqueue(AmiEventEnvelope env);
}
