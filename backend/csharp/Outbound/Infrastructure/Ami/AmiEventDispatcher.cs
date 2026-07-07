using System.Threading.Channels;

namespace Outbound.Infrastructure.Ami;

/// <summary>
/// Async event consumer. The AMI listener calls <see cref="Enqueue"/> and returns
/// immediately; a background task drains the channel and fans each event out to every
/// registered <see cref="IAmiEventHandler"/>. Per-handler exceptions are swallowed so a
/// single misbehaving handler cannot kill the loop.
/// </summary>
public sealed class AmiEventDispatcher : BackgroundService, IAmiEventDispatcher
{
    private readonly Channel<AmiEventEnvelope> _channel =
        Channel.CreateUnbounded<AmiEventEnvelope>(new UnboundedChannelOptions
        {
            SingleReader = true,
            SingleWriter = false,
        });

    private readonly IServiceProvider _sp;
    private readonly ILogger<AmiEventDispatcher> _log;

    public AmiEventDispatcher(IServiceProvider sp, ILogger<AmiEventDispatcher> log)
    {
        _sp = sp;
        _log = log;
    }

    public void Enqueue(AmiEventEnvelope env)
    {
        if (string.IsNullOrWhiteSpace(env.Event)) return;
        _channel.Writer.TryWrite(env);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var handlers = _sp.GetServices<IAmiEventHandler>().ToArray();
        _log.LogInformation("AMI dispatcher started with {Count} handlers.", handlers.Length);
        _log.LogInformation("AMI trace file: {Path}", AmiTrace.Path);
        AmiTrace.Write("DISPATCH", $"dispatcher started with {handlers.Length} handlers");

        await foreach (var env in _channel.Reader.ReadAllAsync(stoppingToken))
        {
            AmiTrace.Event(env);
            foreach (var h in handlers)
            {
                try
                {
                    await h.HandleAsync(env, stoppingToken);
                }
                catch (Exception ex)
                {
                    _log.LogError(ex, "AMI handler {Handler} failed for event {Event}",
                        h.GetType().Name, env.Event);
                }
            }
        }
    }
}
