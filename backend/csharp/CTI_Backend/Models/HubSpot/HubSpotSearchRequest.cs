using System.Text.Json.Serialization;

namespace CTI.Models.HubSpot;

public sealed class HubSpotSearchRequest
{
    [JsonPropertyName("filterGroups")]
    public IReadOnlyList<HubSpotFilterGroup> FilterGroups { get; init; } = Array.Empty<HubSpotFilterGroup>();

    [JsonPropertyName("properties")]
    public IReadOnlyList<string> Properties { get; init; } = Array.Empty<string>();

    [JsonPropertyName("limit")]
    public int Limit { get; init; } = 10;
}

public sealed class HubSpotFilterGroup
{
    [JsonPropertyName("filters")]
    public IReadOnlyList<HubSpotFilter> Filters { get; init; } = Array.Empty<HubSpotFilter>();
}

public sealed class HubSpotFilter
{
    [JsonPropertyName("propertyName")]
    public string PropertyName { get; init; } = string.Empty;

    [JsonPropertyName("operator")]
    public string Operator { get; init; } = "EQ";

    [JsonPropertyName("value")]
    public string Value { get; init; } = string.Empty;
}
