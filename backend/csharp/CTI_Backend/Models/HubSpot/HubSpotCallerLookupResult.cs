namespace CTI.Models.HubSpot;

public sealed class HubSpotCallerLookupResult
{
    public bool Found { get; init; }
    public bool HasMultipleMatches { get; init; }
    public string NormalizedCallerNumber { get; init; } = string.Empty;
    public int TotalMatches { get; init; }
    public HubSpotCallerContact? PrimaryContact { get; init; }
    public IReadOnlyList<HubSpotCallerContact> Contacts { get; init; } = Array.Empty<HubSpotCallerContact>();

    public static HubSpotCallerLookupResult Empty(string normalized) => new()
    {
        Found = false,
        HasMultipleMatches = false,
        NormalizedCallerNumber = normalized,
        TotalMatches = 0,
        PrimaryContact = null,
        Contacts = Array.Empty<HubSpotCallerContact>(),
    };
}

public sealed class HubSpotLookupException : Exception
{
    public string Code { get; }
    public int? HttpStatus { get; }

    public HubSpotLookupException(string code, string message, int? httpStatus = null) : base(message)
    {
        Code = code;
        HttpStatus = httpStatus;
    }
}
