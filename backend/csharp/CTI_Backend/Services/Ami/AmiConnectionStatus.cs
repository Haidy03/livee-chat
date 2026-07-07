namespace CtiBackend.Services.Ami;

public sealed class AmiConnectionStatus
{
    private readonly object _lock = new();
    private bool _connected;
    private DateTime? _lastConnectedAtUtc;
    private DateTime? _lastDisconnectedAtUtc;
    private int _reconnectAttempts;
    private string? _lastError;

    public bool Connected { get { lock (_lock) return _connected; } }
    public DateTime? LastConnectedAtUtc { get { lock (_lock) return _lastConnectedAtUtc; } }
    public DateTime? LastDisconnectedAtUtc { get { lock (_lock) return _lastDisconnectedAtUtc; } }
    public int ReconnectAttempts { get { lock (_lock) return _reconnectAttempts; } }
    public string? LastError { get { lock (_lock) return _lastError; } }

    /// <summary>Raised after the AMI socket has finished logging in.</summary>
    public event Action? ConnectedEvent;

    /// <summary>Raised after the AMI socket disconnects.</summary>
    public event Action<string?>? DisconnectedEvent;

    public void MarkConnected()
    {
        lock (_lock)
        {
            _connected = true;
            _lastConnectedAtUtc = DateTime.UtcNow;
            _lastError = null;
        }
        try { ConnectedEvent?.Invoke(); } catch { /* subscribers must not break the listener */ }
    }

    public void MarkDisconnected(string? error = null)
    {
        lock (_lock)
        {
            _connected = false;
            _lastDisconnectedAtUtc = DateTime.UtcNow;
            if (!string.IsNullOrWhiteSpace(error)) _lastError = error;
        }
        try { DisconnectedEvent?.Invoke(error); } catch { /* swallow */ }
    }

    public void IncrementReconnect()
    {
        lock (_lock) _reconnectAttempts++;
    }

    public object Snapshot()
    {
        lock (_lock)
        {
            return new
            {
                connected = _connected,
                lastConnectedAtUtc = _lastConnectedAtUtc,
                lastDisconnectedAtUtc = _lastDisconnectedAtUtc,
                reconnectAttempts = _reconnectAttempts,
                lastError = _lastError,
            };
        }
    }
}
