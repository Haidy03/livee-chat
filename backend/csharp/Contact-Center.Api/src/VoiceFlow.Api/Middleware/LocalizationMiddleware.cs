using System.Globalization;

namespace VoiceFlow.Api.Middleware;

public sealed class LocalizationMiddleware
{
    private static readonly HashSet<string> SupportedCultures = ["en", "ar"];
    private readonly RequestDelegate _next;

    public LocalizationMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var culture = ResolveLanguage(context);
        CultureInfo.CurrentCulture = new CultureInfo(culture);
        CultureInfo.CurrentUICulture = new CultureInfo(culture);
        context.Items["Language"] = culture;

        await _next(context);
    }

    private static string ResolveLanguage(HttpContext context)
    {
        var acceptLanguage = context.Request.Headers.AcceptLanguage.ToString();

        if (!string.IsNullOrWhiteSpace(acceptLanguage))
        {
            foreach (var lang in acceptLanguage.Split(','))
            {
                var code = lang.Split(';')[0].Trim().Split('-')[0].ToLowerInvariant();
                if (SupportedCultures.Contains(code))
                    return code;
            }
        }

        return "en";
    }
}
