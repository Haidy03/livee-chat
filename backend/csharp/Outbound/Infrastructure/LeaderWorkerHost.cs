using HelperLib.LeaderElection;

namespace Outbound.Infrastructure;

/// <summary>A worker that should run only on the elected leader, one sweep per interval.</summary>
public interface ILeaderGatedWorker
{
    string LeaseId { get; }
    TimeSpan SweepInterval { get; }
    Task RunSweepAsync(CancellationToken ct);
}

/// <summary>
/// Hosts an <see cref="ILeaderGatedWorker"/>: acquires/renews a lease via <see cref="ILeaderElector"/>
/// and only runs the worker's sweep while this node holds leadership. Followers idle and take over
/// on failover. The elector is transient, so each host owns its own lease instance.
/// </summary>
public sealed class LeaderWorkerHost<TWorker> : BackgroundService
    where TWorker : ILeaderGatedWorker
{
    private readonly ILeaderElector _elector;
    private readonly TWorker _worker;
    private readonly ILogger<LeaderWorkerHost<TWorker>> _logger;

    private static readonly TimeSpan ElectorPeriod = TimeSpan.FromSeconds(30);
    private static readonly TimeSpan LeaseTtl = TimeSpan.FromSeconds(60);

    public LeaderWorkerHost(ILeaderElector elector, TWorker worker, ILogger<LeaderWorkerHost<TWorker>> logger)
    {
        _elector = elector;
        _worker = worker;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await _elector.InitializeAsync(stoppingToken);
        _logger.LogInformation("[{Lease}] node {Node} started.", _worker.LeaseId, _elector.NodeId);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var isLeader = await _elector.TryAcquireOrRenewAsync(_worker.LeaseId, LeaseTtl, stoppingToken);
                if (!isLeader)
                {
                    await Task.Delay(ElectorPeriod, stoppingToken);
                    continue;
                }

                await _worker.RunSweepAsync(stoppingToken);
                await Task.Delay(_worker.SweepInterval, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[{Lease}] sweep failed; backing off.", _worker.LeaseId);
                await Task.Delay(ElectorPeriod, stoppingToken);
            }
        }
    }
}
