using System.Net.Sockets;
using System.Text;
using Contact_Center.Worker.Events.CallEvent.Messaging;
using Contact_Center.Worker.Events.CallEvent.Options;
using Microsoft.Extensions.Options;
using VoiceFlow.Contracts.Events;
using VoiceFlow.Core.Entities;
using VoiceFlow.Core.Interfaces.Repositories;

namespace Contact_Center.Worker.Events.CallEvent.Workers;

/// <summary>
/// Maintains a TCP connection to the Asterisk AMI socket, logs in as the voicemail-worker
/// manager account, and listens for UserEvent(VoicemailRecorded) emitted by the dialplan.
/// For each one it inserts the Voicemail document (status "new", so it appears in the inbox
/// immediately) and publishes a VoicemailRecorded event onto the call exchange — which the
/// existing CallMqConsumer then processes (S3 upload + optional transcription). Reconnects
/// automatically; never throws out of ExecuteAsync.
/// </summary>
public sealed class VoicemailAmiListener : BackgroundService
{
    private const string EventName = "VoicemailRecorded";

    private readonly VoicemailAmiOptions _options;
    private readonly string _routingKey;
    private readonly CallEventPublisher _publisher;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<VoicemailAmiListener> _log;

    public VoicemailAmiListener(
        IOptions<VoicemailAmiOptions> options,
        IOptions<CallConsumerOptions> callConsumer,
        CallEventPublisher publisher,
        IServiceScopeFactory scopeFactory,
        ILogger<VoicemailAmiListener> log)
    {
        _options = options.Value;
        _routingKey = string.IsNullOrWhiteSpace(callConsumer.Value.RoutingKey) ? "calls" : callConsumer.Value.RoutingKey;
        _publisher = publisher;
        _scopeFactory = scopeFactory;
        _log = log;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await RunSessionAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _log.LogError(ex, "Voicemail AMI session error; reconnecting in {Delay}s", _options.ReconnectDelaySeconds);
            }

            try
            {
                await Task.Delay(TimeSpan.FromSeconds(Math.Max(1, _options.ReconnectDelaySeconds)), stoppingToken);
            }
            catch (OperationCanceledException) { break; }
        }
    }

    private async Task RunSessionAsync(CancellationToken ct)
    {
        _log.LogInformation("Connecting to Asterisk AMI {Host}:{Port} as {User}", _options.Host, _options.Port, _options.Username);

        using var client = new TcpClient();
        await client.ConnectAsync(_options.Host, _options.Port, ct);
        using var stream = client.GetStream();
        using var reader = new StreamReader(stream, Encoding.UTF8);
        await using var writer = new StreamWriter(stream, new UTF8Encoding(false)) { NewLine = "\r\n", AutoFlush = true };

        await reader.ReadLineAsync(ct); // greeting banner

        await writer.WriteAsync("Action: Login\r\n");
        await writer.WriteAsync($"Username: {_options.Username}\r\n");
        await writer.WriteAsync($"Secret: {_options.Password}\r\n");
        await writer.WriteAsync("Events: on\r\n");
        await writer.WriteAsync("\r\n");
        await writer.FlushAsync(ct);

        _log.LogInformation("Voicemail AMI login sent; listening for UserEvent({EventName})…", EventName);

        var headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        while (!ct.IsCancellationRequested)
        {
            var line = await reader.ReadLineAsync(ct);
            if (line is null) throw new IOException("AMI stream closed");

            if (line.Length == 0)
            {
                if (headers.Count > 0)
                {
                    try { await HandleMessageAsync(headers, ct); }
                    catch (Exception ex) { _log.LogError(ex, "Failed handling AMI message"); }
                    headers.Clear();
                }
                continue;
            }

            var idx = line.IndexOf(':');
            if (idx > 0)
            {
                var key = line[..idx].Trim();
                var val = line[(idx + 1)..].Trim();
                headers[key] = val;
            }
        }
    }

    private async Task HandleMessageAsync(IReadOnlyDictionary<string, string> h, CancellationToken ct)
    {
        if (!h.TryGetValue("Event", out var ev) || !string.Equals(ev, "UserEvent", StringComparison.OrdinalIgnoreCase))
            return;
        if (!h.TryGetValue("UserEvent", out var name) || !string.Equals(name, EventName, StringComparison.OrdinalIgnoreCase))
            return;

        string Get(string k) => h.TryGetValue(k, out var v) ? v : string.Empty;

        var tenantId = Get("TenantId");
        var ownerId = Get("OwnerId");
        var recordingPath = Get("RecordingPath");

        if (string.IsNullOrWhiteSpace(tenantId) || string.IsNullOrWhiteSpace(ownerId))
        {
            _log.LogWarning("VoicemailRecorded UserEvent missing TenantId/OwnerId; ignoring.");
            return;
        }

        var ownerType = string.IsNullOrWhiteSpace(Get("OwnerType")) ? "flow" : Get("OwnerType");
        var transcription = Get("Transcription") is "1" or "true";
        int.TryParse(Get("Duration"), out var duration);

        string voicemailId;
        using (var scope = _scopeFactory.CreateScope())
        {
            var repo = scope.ServiceProvider.GetRequiredService<IVoicemailRepository>();
            var vm = new Voicemail
            {
                TenantId = tenantId,
                OwnerType = ownerType,
                OwnerId = ownerId,
                Mailbox = $"vm-{ownerId}",
                FlowId = NullIfBlank(Get("FlowId")),
                NodeId = NullIfBlank(Get("NodeId")),
                Uuid = NullIfBlank(Get("Uuid")),
                CallerIdNumber = NullIfBlank(Get("Caller")),
                DestinationNumber = NullIfBlank(Get("Destination")),
                RecordingPath = recordingPath,
                DurationSeconds = duration,
                TranscriptionRequested = transcription,
                Status = "new",
                Timestamp = DateTime.UtcNow,
            };
            await repo.InsertAsync(vm, ct);
            voicemailId = vm.Id;
        }

        await _publisher.PublishAsync(new CallTerminatedEvent
        {
            Id = voicemailId,
            TenantId = tenantId,
            OwnerType = ownerType,
            OwnerId = ownerId,
            RecordingPath = recordingPath,
            Transcription = transcription,
            Timestamp = DateTime.UtcNow,
            Event = EventName,
        }, _routingKey, ct);

        _log.LogInformation(
            "Ingested voicemail {Id} owner {OwnerType}:{OwnerId} and published for processing.",
            voicemailId, ownerType, ownerId);
    }

    private static string? NullIfBlank(string? v) => string.IsNullOrWhiteSpace(v) ? null : v;
}
