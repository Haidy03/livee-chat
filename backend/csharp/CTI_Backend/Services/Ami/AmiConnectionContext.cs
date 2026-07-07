namespace CtiBackend.Services.Ami;

/// <summary>
/// Identifies the AMI connection that originated an event. There is exactly
/// one AMI connection per process (managed by <see cref="AmiListenerHostedService"/>),
/// so this is registered as a singleton populated from <see cref="CtiBackend.Options.QueueMonitoringOptions"/>.
/// </summary>
public sealed class AmiConnectionContext
{
    public string TenantId { get; init; } = "default";
    public string ServerId { get; init; } = "asterisk-1";
    public string ConnectionName { get; init; } = "default";
}
