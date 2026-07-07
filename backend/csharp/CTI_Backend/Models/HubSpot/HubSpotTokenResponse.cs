using System.Text.Json.Serialization;

namespace CtiBackend.Models.HubSpot;

public sealed class HubSpotTokenResponse
{
    [JsonPropertyName("access_token")]
    public string AccessToken { get; set; } = string.Empty;

    [JsonPropertyName("refresh_token")]
    public string? RefreshToken { get; set; }

    [JsonPropertyName("expires_in")]
    public int ExpiresIn { get; set; }

    [JsonPropertyName("token_type")]
    public string? TokenType { get; set; }
}

public sealed class HubSpotTokenAccountInfo
{
    [JsonPropertyName("hub_id")]
    public long? HubId { get; set; }

    [JsonPropertyName("hub_domain")]
    public string? HubDomain { get; set; }

    [JsonPropertyName("scopes")]
    public List<string>? Scopes { get; set; }

    [JsonPropertyName("user")]
    public string? User { get; set; }

    [JsonPropertyName("user_id")]
    public long? UserId { get; set; }
}
