namespace VoiceFlow.Application.Common;

public interface ITenantContext
{
    string TenantId { get; }
    bool IsResolved { get; }
}
