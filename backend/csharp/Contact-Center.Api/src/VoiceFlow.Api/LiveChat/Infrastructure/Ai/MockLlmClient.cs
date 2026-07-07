using System.Text.Json;

namespace VoiceFlow.Api.LiveChat.Infrastructure.Ai;

/// <summary>
/// Deterministic fake used for local dev/tests. Returns a plausible JSON
/// payload based on which fields the prompt mentions.
/// </summary>
public sealed class MockLlmClient : ILlmClient
{
    public Task<string> GenerateJsonAsync(string systemPrompt, string userPrompt, CancellationToken ct = default)
    {
        // Heuristics: look at the requested keys in the user prompt template.
        var p = userPrompt.ToLowerInvariant();
        object payload;

        if (p.Contains("\"suggestedreplies\"") || p.Contains("suggested replies") || p.Contains("suggest reply") || p.Contains("improve") || p.Contains("professional") || p.Contains("shorter") || p.Contains("translate"))
        {
            payload = new
            {
                suggestedReplies = new[]
                {
                    "Thanks for reaching out — could you please share your order number so I can look this up?",
                    "Happy to help! Could you confirm the email address on the account?",
                    "I understand. Let me check this for you — one moment please.",
                },
                confidence = 0.8m,
                warning = (string?)null,
            };
        }
        else if (p.Contains("\"summary\"") || p.Contains("summarize"))
        {
            payload = new
            {
                summary = "The customer is following up on an order and has not yet provided the order number.",
                confidence = 0.85m,
                warning = (string?)null,
            };
        }
        else if (p.Contains("\"detectedintent\"") || p.Contains("intent"))
        {
            payload = new
            {
                detectedIntent = "Order Status",
                suggestedTags = new[] { "Order Tracking", "Customer Support" },
                confidence = 0.82m,
                warning = (string?)null,
            };
        }
        else if (p.Contains("\"suggestedtags\"") || p.Contains("tags"))
        {
            payload = new
            {
                suggestedTags = new[] { "Support", "Follow Up" },
                confidence = 0.7m,
                warning = (string?)null,
            };
        }
        else if (p.Contains("\"nextactions\"") || p.Contains("next best action") || p.Contains("next action"))
        {
            payload = new
            {
                nextActions = new[]
                {
                    "Ask the customer for the order number.",
                    "Check the order status in the CRM.",
                    "If delayed, apologize and share the expected delivery time.",
                },
                confidence = 0.78m,
                warning = (string?)null,
            };
        }
        else
        {
            payload = new { warning = "mock_no_match", confidence = 0.0m };
        }

        return Task.FromResult(JsonSerializer.Serialize(payload));
    }
}
