using Microsoft.Extensions.Options;

namespace CtiBackend.Options;

public sealed class HubSpotOptions
{
    public const string SectionName = "HubSpot";

    public string ClientId { get; set; } = string.Empty;
    public string ClientSecret { get; set; } = string.Empty;
    public string AuthorizationUrl { get; set; } = "https://app.hubspot.com/oauth/authorize";
    public string TokenUrl { get; set; } = "https://api.hubapi.com/oauth/v1/token";
    public string RedirectUri { get; set; } = string.Empty;
    public string FrontendSuccessUrl { get; set; } = string.Empty;
    public string FrontendFailureUrl { get; set; } = string.Empty;
    public List<string> AllowedFrontendHosts { get; set; } = new();
    public List<string> Scopes { get; set; } = new();
    public int StateExpirationMinutes { get; set; } = 10;
    public int AccessTokenSafetyMarginSeconds { get; set; } = 90;
    public string MongoStatesCollection { get; set; } = "hubspot_oauth_states";
    public string MongoIntegrationsCollection { get; set; } = "hubspot_integrations";
    public string MongoDataProtectionCollection { get; set; } = "dataprotection_keys";
    // Caller-lookup options
    public int CallerLookupCacheSeconds { get; set; } = 30;
    public int SearchResultLimit { get; set; } = 10;
    public string DefaultCountryCode { get; set; } = "SA";
    public string RequiredContactScope { get; set; } = "crm.objects.contacts.read";
}

public sealed class HubSpotOptionsValidator : IValidateOptions<HubSpotOptions>
{
    private readonly IHostEnvironment _env;
    public HubSpotOptionsValidator(IHostEnvironment env) => _env = env;

    public ValidateOptionsResult Validate(string? name, HubSpotOptions o)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(o.ClientId)) errors.Add("HubSpot:ClientId is required.");
        if (string.IsNullOrWhiteSpace(o.ClientSecret)) errors.Add("HubSpot:ClientSecret is required.");
        if (string.IsNullOrWhiteSpace(o.AuthorizationUrl)) errors.Add("HubSpot:AuthorizationUrl is required.");
        if (string.IsNullOrWhiteSpace(o.TokenUrl)) errors.Add("HubSpot:TokenUrl is required.");
        if (string.IsNullOrWhiteSpace(o.RedirectUri)) errors.Add("HubSpot:RedirectUri is required.");
        if (string.IsNullOrWhiteSpace(o.FrontendSuccessUrl)) errors.Add("HubSpot:FrontendSuccessUrl is required.");
        if (string.IsNullOrWhiteSpace(o.FrontendFailureUrl)) errors.Add("HubSpot:FrontendFailureUrl is required.");
        if (o.Scopes is null || o.Scopes.Count == 0) errors.Add("HubSpot:Scopes must contain at least one scope.");
        if (o.StateExpirationMinutes < 1 || o.StateExpirationMinutes > 60)
            errors.Add("HubSpot:StateExpirationMinutes must be between 1 and 60.");

        foreach (var (label, value) in new[]
        {
            ("RedirectUri", o.RedirectUri),
            ("FrontendSuccessUrl", o.FrontendSuccessUrl),
            ("FrontendFailureUrl", o.FrontendFailureUrl),
        })
        {
            if (string.IsNullOrWhiteSpace(value)) continue;
            if (!Uri.TryCreate(value, UriKind.Absolute, out var uri))
            {
                errors.Add($"HubSpot:{label} is not a valid absolute URL.");
                continue;
            }
            if (uri.Host.Contains(".."))
                errors.Add($"HubSpot:{label} contains a malformed host (double dots): {uri.Host}.");
            if (!_env.IsDevelopment() && uri.Scheme != Uri.UriSchemeHttps)
                errors.Add($"HubSpot:{label} must use HTTPS in non-development environments.");
        }

        if (o.AllowedFrontendHosts is { Count: > 0 })
        {
            foreach (var (label, value) in new[]
            {
                ("FrontendSuccessUrl", o.FrontendSuccessUrl),
                ("FrontendFailureUrl", o.FrontendFailureUrl),
            })
            {
                if (Uri.TryCreate(value, UriKind.Absolute, out var uri) &&
                    !o.AllowedFrontendHosts.Contains(uri.Host, StringComparer.OrdinalIgnoreCase))
                {
                    errors.Add($"HubSpot:{label} host '{uri.Host}' is not in AllowedFrontendHosts.");
                }
            }
        }

        return errors.Count == 0 ? ValidateOptionsResult.Success : ValidateOptionsResult.Fail(errors);
    }
}
