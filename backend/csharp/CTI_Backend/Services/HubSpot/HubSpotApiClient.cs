using System.Net;
using System.Net.Http.Headers;

namespace CtiBackend.Services.HubSpot;

/// <summary>
/// Tenant-aware HubSpot HTTP client. Always obtains the access token
/// via <see cref="IHubSpotTokenProvider"/>; on a single 401 forces a
/// token refresh and retries the request once. Respects Retry-After
/// on 429 (single retry).
/// </summary>
public sealed class HubSpotApiClient
{
    private readonly IHttpClientFactory _http;
    private readonly IHubSpotTokenProvider _tokens;
    private readonly ILogger<HubSpotApiClient> _log;

    public HubSpotApiClient(IHttpClientFactory http, IHubSpotTokenProvider tokens, ILogger<HubSpotApiClient> log)
    {
        _http = http; _tokens = tokens; _log = log;
    }

    public async Task<HttpResponseMessage> SendAsync(string tenantId, HttpRequestMessage request, CancellationToken ct = default)
    {
        var client = _http.CreateClient("hubspot");
        var token = await _tokens.GetValidAccessTokenAsync(tenantId, ct);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var resp = await client.SendAsync(request);

        if (resp.StatusCode == HttpStatusCode.Unauthorized)
        {
            resp.Dispose();
            await _tokens.InvalidateAsync(tenantId, ct);
            var retryToken = await _tokens.GetValidAccessTokenAsync(tenantId, ct);
            var retry = await CloneAsync(request);
            retry.Headers.Authorization = new AuthenticationHeaderValue("Bearer", retryToken);
            resp = await client.SendAsync(retry, ct);
        }
        else if (resp.StatusCode == (HttpStatusCode)429)
        {
            var delay = resp.Headers.RetryAfter?.Delta ?? TimeSpan.FromSeconds(1);
            resp.Dispose();
            await Task.Delay(delay, ct);
            var retry = await CloneAsync(request);
            retry.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            resp = await client.SendAsync(retry, ct);
        }

        return resp;
    }

    private static async Task<HttpRequestMessage> CloneAsync(HttpRequestMessage src)
    {
        var clone = new HttpRequestMessage(src.Method, src.RequestUri);
        if (src.Content is not null)
        {
            var bytes = await src.Content.ReadAsByteArrayAsync();
            clone.Content = new ByteArrayContent(bytes);
            foreach (var h in src.Content.Headers) clone.Content.Headers.TryAddWithoutValidation(h.Key, h.Value);
        }
        foreach (var h in src.Headers) clone.Headers.TryAddWithoutValidation(h.Key, h.Value);
        return clone;
    }
}
