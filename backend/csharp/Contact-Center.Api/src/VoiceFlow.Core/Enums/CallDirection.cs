namespace VoiceFlow.Core.Enums;

// Canonical call direction values across the system.
// API JSON serialization (camelCase) produces "inbound" / "outbound" / "internal".
// MongoDB persistence is configured to write the same lowercase strings (see
// VoiceFlow.Infrastructure.Persistence.Serializers.CallDirectionSerializer).
public enum CallDirection
{
    Inbound,
    Outbound,
    Internal
}
