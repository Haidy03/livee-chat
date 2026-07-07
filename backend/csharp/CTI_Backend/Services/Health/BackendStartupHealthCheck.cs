// ============================================================================
// Startup health probe for Redis + MongoDB. Performs a round-trip write/read
// on each backend at app start, logs the outcome, and exposes the latest
// result via IBackendHealthState so the /api/cti/health endpoint can report it.
// ============================================================================

using System.Diagnostics;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Driver;
using StackExchange.Redis;
using CtiBackend.Options;

namespace CtiBackend.Services.Health;

public sealed class BackendProbeResult
{
    public bool Ok { get; init; }
    public long LatencyMs { get; init; }
    public string? Error { get; init; }
    public DateTime CheckedAtUtc { get; init; } = DateTime.UtcNow;
}

public interface IBackendHealthState
{
    BackendProbeResult? Redis { get; }
    BackendProbeResult? Mongo { get; }
    BackendProbeResult? LiveCallRoundTrip { get; }
    void SetRedis(BackendProbeResult r);
    void SetMongo(BackendProbeResult r);
    void SetLiveCallRoundTrip(BackendProbeResult r);
}

public sealed class BackendHealthState : IBackendHealthState
{
    public BackendProbeResult? Redis { get; private set; }
    public BackendProbeResult? Mongo { get; private set; }
    public BackendProbeResult? LiveCallRoundTrip { get; private set; }
    public void SetRedis(BackendProbeResult r) => Redis = r;
    public void SetMongo(BackendProbeResult r) => Mongo = r;
    public void SetLiveCallRoundTrip(BackendProbeResult r) => LiveCallRoundTrip = r;
}

/// <summary>
/// Runs once at startup. Verifies Redis PING + SET/GET, MongoDB ping +
/// insert into a probe collection, then exercises ILiveCallRegistry by
/// recording a synthetic LiveCall and reading it back via GetSnapshotAsync.
/// All failures are logged but never crash the host.
/// </summary>
public sealed class BackendStartupHealthCheck : IHostedService
{
    private readonly IServiceProvider _sp;
    private readonly ILogger<BackendStartupHealthCheck> _log;
    private readonly IBackendHealthState _state;

    public BackendStartupHealthCheck(
        IServiceProvider sp,
        ILogger<BackendStartupHealthCheck> log,
        IBackendHealthState state)
    {
        _sp = sp;
        _log = log;
        _state = state;
    }

    public Task StartAsync(CancellationToken ct)
    {
        _ = Task.Run(() => RunAsync(ct), ct);
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken ct) => Task.CompletedTask;

    private async Task RunAsync(CancellationToken ct)
    {
        await ProbeRedisAsync(ct);
        await ProbeMongoAsync(ct);
        await ProbeLiveCallRoundTripAsync(ct);
    }

    private async Task ProbeRedisAsync(CancellationToken ct)
    {
        var sw = Stopwatch.StartNew();
        try
        {
            var mux = _sp.GetRequiredService<IConnectionMultiplexer>();
            var db = mux.GetDatabase();
            await db.PingAsync();
            var key = $"cti:probe:{Guid.NewGuid():N}";
            await db.StringSetAsync(key, "ok", TimeSpan.FromMinutes(1));
            var got = (string?)await db.StringGetAsync(key);
            await db.KeyDeleteAsync(key);
            if (got != "ok") throw new Exception($"unexpected probe value: {got ?? "<null>"}");
            sw.Stop();
            _state.SetRedis(new BackendProbeResult { Ok = true, LatencyMs = sw.ElapsedMilliseconds });
            _log.LogInformation("Redis probe OK in {Ms} ms", sw.ElapsedMilliseconds);
        }
        catch (Exception ex)
        {
            sw.Stop();
            _state.SetRedis(new BackendProbeResult { Ok = false, LatencyMs = sw.ElapsedMilliseconds, Error = ex.Message });
            _log.LogError(ex, "Redis probe FAILED");
        }
    }

    private async Task ProbeMongoAsync(CancellationToken ct)
    {
        var sw = Stopwatch.StartNew();
        try
        {
            var client = _sp.GetRequiredService<IMongoClient>();
            var opts = _sp.GetRequiredService<IOptions<MongoOptions>>().Value;
            var dbm = client.GetDatabase(opts.Database);
            await dbm.RunCommandAsync<BsonDocument>(new BsonDocument("ping", 1), cancellationToken: ct);
            var col = dbm.GetCollection<BsonDocument>("cti_probe");
            var doc = new BsonDocument { { "_id", ObjectId.GenerateNewId() }, { "at", DateTime.UtcNow }, { "kind", "startup-probe" } };
            await col.InsertOneAsync(doc, cancellationToken: ct);
            await col.DeleteOneAsync(Builders<BsonDocument>.Filter.Eq("_id", doc["_id"]), ct);
            sw.Stop();
            _state.SetMongo(new BackendProbeResult { Ok = true, LatencyMs = sw.ElapsedMilliseconds });
            _log.LogInformation("MongoDB probe OK in {Ms} ms (db={Db})", sw.ElapsedMilliseconds, opts.Database);
        }
        catch (Exception ex)
        {
            sw.Stop();
            _state.SetMongo(new BackendProbeResult { Ok = false, LatencyMs = sw.ElapsedMilliseconds, Error = ex.Message });
            _log.LogError(ex, "MongoDB probe FAILED");
        }
    }

    private async Task ProbeLiveCallRoundTripAsync(CancellationToken ct)
    {
        var sw = Stopwatch.StartNew();
        const string probeTenant = "__probe__";
        var callId = $"probe-{Guid.NewGuid():N}";
        try
        {
            var registry = _sp.GetRequiredService<CtiBackend.Services.State.UsersMap.ILiveCallRegistry>();
            var now = DateTimeOffset.UtcNow.ToString("O");
            var call = new CtiBackend.Services.State.UsersMap.LiveCall
            {
                Id = callId,
                CallId = callId,
                TenantId = probeTenant,
                Name = "Startup Probe",
                MaskedNumber = "+0000000000",
                State = CtiBackend.Services.State.UsersMap.CallState.CallStart,
                EnteredStateAt = now,
                CallStartedAt = now,
            };
            await registry.RecordStateAsync(call, ct);
            var snap = await registry.GetSnapshotAsync(probeTenant, ct);
            var found = snap.Calls.Any(c => c.CallId == callId);
            await registry.RemoveAsync(probeTenant, callId, "completed", ct);
            if (!found) throw new Exception("probe call not visible in snapshot after write");
            sw.Stop();
            _state.SetLiveCallRoundTrip(new BackendProbeResult { Ok = true, LatencyMs = sw.ElapsedMilliseconds });
            _log.LogInformation("LiveCall round-trip OK in {Ms} ms", sw.ElapsedMilliseconds);
        }
        catch (Exception ex)
        {
            sw.Stop();
            _state.SetLiveCallRoundTrip(new BackendProbeResult { Ok = false, LatencyMs = sw.ElapsedMilliseconds, Error = ex.Message });
            _log.LogError(ex, "LiveCall round-trip FAILED");
        }
    }
}
