namespace VoiceFlow.Contracts.Calls;

public sealed class UpdateCallRequest
{
    public string? Notes { get; set; }
    public List<string>? TagIds { get; set; }
    public string? Summary { get; set; }
    public string? SummaryAccuracyFeedback { get; set; }
}
