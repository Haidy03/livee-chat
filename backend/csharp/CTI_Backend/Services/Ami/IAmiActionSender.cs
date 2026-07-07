namespace CtiBackend.Services.Ami;

/// <summary>
/// Writes AMI Actions back to the existing AMI socket maintained by
/// <see cref="AmiListenerHostedService"/>. Implemented as a singleton that
/// the listener wires its current <see cref="System.IO.StreamWriter"/> into
/// after each successful login. No second AMI connection is ever opened.
/// </summary>
public interface IAmiActionSender
{
    bool IsReady { get; }
    Task SendAsync(IReadOnlyList<KeyValuePair<string, string>> fields, CancellationToken ct);
}

public sealed class AmiActionSender : IAmiActionSender
{
    private readonly SemaphoreSlim _gate = new(1, 1);
    private StreamWriter? _writer;

    public bool IsReady => _writer != null;

    /// <summary>Called by the AMI listener after a successful login.</summary>
    public void AttachWriter(StreamWriter writer)
    {
        _writer = writer;
    }

    /// <summary>Called by the AMI listener when the socket closes.</summary>
    public void DetachWriter(StreamWriter writer)
    {
        if (ReferenceEquals(_writer, writer)) _writer = null;
    }

    public async Task SendAsync(IReadOnlyList<KeyValuePair<string, string>> fields, CancellationToken ct)
    {
        var writer = _writer;
        if (writer == null) throw new InvalidOperationException("AMI connection is not ready.");

        await _gate.WaitAsync(ct);
        try
        {
            foreach (var kv in fields)
                await writer.WriteAsync($"{kv.Key}: {kv.Value}\r\n");
            await writer.WriteAsync("\r\n");
            await writer.FlushAsync();
        }
        finally
        {
            _gate.Release();
        }
    }
}
