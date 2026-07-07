namespace VoiceFlow.Contracts.Calls;

public sealed class CallSearchResponse
{
    public IReadOnlyList<CallResponse> Items { get; init; } = [];
    public long Total { get; init; }
    public int Page { get; init; }
    public int PageSize { get; init; }

    public static CallSearchResponse Create(IReadOnlyList<CallResponse> items, int page, int pageSize, long total) =>
        new() { Items = items, Page = page, PageSize = pageSize, Total = total };
}
