namespace VoiceFlow.Contracts.Calls;

public sealed class CallFilterOptions
{
    public IReadOnlyList<CallFilterAgentOption> Agents { get; init; } = [];
    public IReadOnlyList<CallFilterGroupOption> Groups { get; init; } = [];
    public IReadOnlyList<CallFilterTagOption> Tags { get; init; } = [];
    public IReadOnlyList<string> AbandonmentReasons { get; init; } = [];
    public IReadOnlyList<HandledByFilter> HandledByOptions { get; init; } = [];
}

public sealed class CallFilterAgentOption
{
    public string Id { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
}

public sealed class CallFilterGroupOption
{
    public string Id { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
}

public sealed class CallFilterTagOption
{
    public string Id { get; init; } = string.Empty;
    public string Label { get; init; } = string.Empty;
    public string? Color { get; init; }
}
