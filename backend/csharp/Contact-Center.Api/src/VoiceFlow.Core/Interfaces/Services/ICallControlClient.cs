namespace VoiceFlow.Core.Interfaces.Services;

/// <summary>
/// Issues supervisor actions (listen / whisper / barge / hangup / transfer)
/// to the underlying call platform (FreeSWITCH ESL or Asterisk AMI).
/// </summary>
public interface ICallControlClient
{
    Task<bool> ListenAsync(string tenantId, string callId, CancellationToken ct);
    Task<bool> WhisperAsync(string tenantId, string callId, CancellationToken ct);
    Task<bool> BargeAsync(string tenantId, string callId, CancellationToken ct);
    Task<bool> HangupAsync(string tenantId, string callId, CancellationToken ct);
    Task<bool> TransferAsync(string tenantId, string callId, string targetType, string? targetId, string? targetNumber, CancellationToken ct);
}
