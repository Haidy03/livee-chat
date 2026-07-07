namespace VoiceFlow.Contracts.Flows;

public sealed class FlowValidationResponse
{
    public bool IsValid { get; init; }
    public List<string> Errors { get; init; } = [];
    public List<string> Warnings { get; init; } = [];
}
