using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using VoiceFlow.Api.LiveChat.Config;

namespace VoiceFlow.Api.LiveChat.Infrastructure.Ai;

public sealed class OpenAiLlmClient : ILlmClient
{
    private readonly IHttpClientFactory _http;
    private readonly IOptions<AiSuggestOptions> _options;

    public OpenAiLlmClient(IHttpClientFactory http, IOptions<AiSuggestOptions> options)
    {
        _http = http;
        _options = options;
    }

    public async Task<string> GenerateJsonAsync(string systemPrompt, string userPrompt, CancellationToken ct = default)
    {
        var opts = _options.Value;
        if (string.IsNullOrWhiteSpace(opts.ApiKey))
            throw new InvalidOperationException("AiSuggest:ApiKey is not configured.");

        var client = _http.CreateClient("aisuggest");
        client.Timeout = TimeSpan.FromSeconds(Math.Max(5, opts.TimeoutSeconds));
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", opts.ApiKey);

        var body = new
        {
            model = opts.Model,
            temperature = 0.3,
            response_format = new { type = "json_object" },
            messages = new object[]
            {
                new { role = "system", content = systemPrompt },
                new { role = "user", content = userPrompt },
            },
        };

        var baseUrl = opts.BaseUrl.TrimEnd('/');
        using var req = new HttpRequestMessage(HttpMethod.Post, $"{baseUrl}/chat/completions")
        {
            Content = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json"),
        };

        using var resp = await client.SendAsync(req, ct);
        var text = await resp.Content.ReadAsStringAsync(ct);
        if (!resp.IsSuccessStatusCode)
            throw new HttpRequestException($"LLM error {(int)resp.StatusCode}: {text}");

        using var doc = JsonDocument.Parse(text);
        var content = doc.RootElement
            .GetProperty("choices")[0]
            .GetProperty("message")
            .GetProperty("content")
            .GetString();
        return content ?? "{}";
    }
}
