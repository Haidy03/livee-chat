using VoiceFlow.Core.ValueObjects;

namespace VoiceFlow.Contracts.Accounts;

public sealed class AccountResponse
{
    public string Id { get; init; } = string.Empty;
    public string OrgName { get; init; } = string.Empty;
    public string DefaultCountry { get; init; } = string.Empty;
    public string NumberFormat { get; init; } = string.Empty;
    public bool AutoAnswer { get; init; }
    public int AutoAnswerSecs { get; init; }
    public bool AutoTagging { get; init; }
    public bool NotifyOnAgentChanges { get; init; }
    public List<PhoneNumber> PhoneNumbers { get; set; } = [];

    public DateTime CreatedAt { get; init; }
    public DateTime UpdatedAt { get; init; }
}
