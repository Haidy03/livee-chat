namespace VoiceFlow.Contracts.Accounts;

public sealed class UpdateAccountRequest
{
    public string? OrgName { get; set; }
    public string? DefaultCountry { get; set; }
    public string? NumberFormat { get; set; }
    public bool? AutoAnswer { get; set; }
    public int? AutoAnswerSecs { get; set; }
    public bool? AutoTagging { get; set; }
    public string? DialerUrl { get; set; }
    public string? DialerMethod { get; set; }
    public bool? NotifyOnAgentChanges { get; set; }
}
