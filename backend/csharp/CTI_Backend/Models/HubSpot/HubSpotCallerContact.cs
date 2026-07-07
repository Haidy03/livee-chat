namespace CTI.Models.HubSpot;

public sealed class HubSpotCallerContact
{
    public string HubSpotContactId { get; init; } = string.Empty;
    public string? FirstName { get; init; }
    public string? LastName { get; init; }
    public string? FullName { get; init; }
    public string? Email { get; init; }
    public string? Phone { get; init; }
    public string? MobilePhone { get; init; }
    public string? Company { get; init; }
    public string? JobTitle { get; init; }
    public string? LifecycleStage { get; init; }
    public string? HubSpotOwnerId { get; init; }
    public DateTimeOffset? CreatedAt { get; init; }
    public DateTimeOffset? UpdatedAt { get; init; }
}
