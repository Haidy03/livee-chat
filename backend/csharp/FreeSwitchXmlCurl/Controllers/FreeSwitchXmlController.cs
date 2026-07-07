using Microsoft.AspNetCore.Mvc;
using VoiceFlow.FreeSwitchXmlCurl.Services;

namespace VoiceFlow.FreeSwitchXmlCurl.Controllers;

[ApiController]
[Route("api/freeswitch")]
public sealed class FreeSwitchXmlController : ControllerBase
{
    private readonly IDialplanService _dialplan;
    private readonly ILogger<FreeSwitchXmlController> _log;

    public FreeSwitchXmlController(IDialplanService dialplan, ILogger<FreeSwitchXmlController> log)
    {
        _dialplan = dialplan;
        _log = log;
    }

    [HttpPost("xml")]
    [Consumes("application/x-www-form-urlencoded")]
    [Produces("text/xml")]
    public async Task<IActionResult> Xml(CancellationToken ct)
    {
        var parameters = new Dictionary<string, string>(StringComparer.Ordinal);
        if (Request.HasFormContentType)
        {
            var form = await Request.ReadFormAsync(ct);
            foreach (var kv in form)
                parameters[kv.Key] = kv.Value.ToString();
        }

        var xml = await _dialplan.HandleAsync(parameters, ct);
        return Content(xml, "text/xml", System.Text.Encoding.UTF8);
    }
}
