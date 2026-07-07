using System.Net.Sockets;
using System.Text;
using CtiBackend.Options;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CtiBackend.Services.Ami;

/// <summary>
/// Long-running hosted service that maintains a TCP connection to the
/// Asterisk AMI socket, logs in, and streams events into the dispatcher.
/// Reconnects automatically on failure; never throws out of ExecuteAsync.
/// </summary>
public sealed class AmiListenerHostedService : BackgroundService
{
    private readonly AmiOptions _options;
    private readonly IAmiMessageParser _parser;
    private readonly IAmiEventDispatcher _dispatcher;
    private readonly AmiConnectionStatus _status;
    private readonly AmiActionSender _actionSender;
    private readonly ILogger<AmiListenerHostedService> _log;

    public AmiListenerHostedService(
        IOptions<AmiOptions> options,
        IAmiMessageParser parser,
        IAmiEventDispatcher dispatcher,
        AmiConnectionStatus status,
        AmiActionSender actionSender,
        ILogger<AmiListenerHostedService> log)
    {
        _options = options.Value;
        _parser = parser;
        _dispatcher = dispatcher;
        _status = status;
        _actionSender = actionSender;
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
                _status.MarkDisconnected(ex.Message);
                _log.LogError(ex, "AMI session error; reconnecting in {Delay}s",
                    _options.ReconnectDelaySeconds);
            }

            try
            {
                _status.IncrementReconnect();
                await Task.Delay(TimeSpan.FromSeconds(Math.Max(1, _options.ReconnectDelaySeconds)), stoppingToken);
            }
            catch (OperationCanceledException) { break; }
        }
    }

    private async Task RunSessionAsync(CancellationToken ct)
    {
        _log.LogInformation("Connecting to AMI {Host}:{Port} as {User}",
            _options.Host, _options.Port, _options.Username);

        using var client = new TcpClient();
        await client.ConnectAsync(_options.Host, _options.Port, ct);
        using var stream = client.GetStream();
        using var reader = new StreamReader(stream, Encoding.UTF8);
        await using var writer = new StreamWriter(stream, new UTF8Encoding(false)) { NewLine = "\r\n", AutoFlush = true };

        // Banner line, e.g. "Asterisk Call Manager/x.y.z"
        var banner = await reader.ReadLineAsync(ct);
        _log.LogInformation("AMI banner: {Banner}", banner);

        // Login action
        await writer.WriteAsync("Action: Login\r\n");
        await writer.WriteAsync($"Username: {_options.Username}\r\n");
        await writer.WriteAsync($"Secret: {_options.Password}\r\n");
        await writer.WriteAsync($"Events: {(_options.EnableEvents ? "on" : "off")}\r\n");
        await writer.WriteAsync("\r\n");
        await writer.FlushAsync();

        _actionSender.AttachWriter(writer);
        _status.MarkConnected();
        _log.LogInformation("AMI login sent; reading events…");

        try
        {
            var buffer = new StringBuilder();
            while (!ct.IsCancellationRequested)
            {
            var line = await reader.ReadLineAsync(ct);
            if (line is null)
            {
                throw new IOException("AMI stream closed");
            }

            if (line.Length == 0)
            {
                if (buffer.Length == 0) continue;
                var raw = buffer.ToString();
                buffer.Clear();
                try
                {
                    var env = _parser.Parse(raw);
                    _dispatcher.Enqueue(env);
                }
                catch (Exception ex)
                {
                    _log.LogError(ex, "Failed to parse AMI message");
                }
            }
            else
            {
                buffer.AppendLine(line);
            }
            }
        }
        finally
        {
            _actionSender.DetachWriter(writer);
        }
    }
}
