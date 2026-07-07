using VoiceFlow.Contracts.Common;

namespace VoiceFlow.Contracts.Contacts;

public sealed class ContactSearchRequest : PaginationRequest
{
    public string? Query { get; set; }
    public List<string>? TagIds { get; set; }
}
