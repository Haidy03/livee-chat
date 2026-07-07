using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Options;
using VoiceFlow.Application.Options;

namespace VoiceFlow.Api.Filters;

/// <summary>
/// Validates the shared <c>X-Ingest-Token</c> header against
/// <see cref="UsersMapOptions.IngestToken"/>. Bypasses regular JWT auth so
/// dialplan callouts can post without user context.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public sealed class IngestTokenAuthAttribute : Attribute, IAsyncAuthorizationFilter
{
    public Task OnAuthorizationAsync(AuthorizationFilterContext context)
    {
        var opts = context.HttpContext.RequestServices.GetRequiredService<IOptions<UsersMapOptions>>().Value;
        if (string.IsNullOrEmpty(opts.IngestToken))
        {
            context.Result = new ObjectResult(new { error = "ingest disabled: no token configured" }) { StatusCode = 503 };
            return Task.CompletedTask;
        }

        var provided = context.HttpContext.Request.Headers["X-Ingest-Token"].ToString();
        if (!string.Equals(provided, opts.IngestToken, StringComparison.Ordinal))
        {
            context.Result = new UnauthorizedResult();
        }
        return Task.CompletedTask;
    }
}
