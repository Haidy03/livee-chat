using VoiceFlow.Contracts.Common;
using VoiceFlow.Core.Enums;

namespace VoiceFlow.Contracts.Calls;

public sealed class CallSearchRequest : PaginationRequest
{
    public CallDirection? Direction { get; set; }
    public CallStatus? Status { get; set; }
    public DateTime? From { get; set; }
    public DateTime? To { get; set; }
    public string? Caller { get; set; }
    public List<string>? TagIds { get; set; }

    /// <summary>When true, only rows with a non-empty SIP/external CallId.</summary>
    public bool? SoftphoneOnly { get; set; }

    public string? UserId { get; set; }
}
