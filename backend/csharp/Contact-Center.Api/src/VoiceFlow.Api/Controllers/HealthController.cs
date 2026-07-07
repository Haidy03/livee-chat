using Microsoft.AspNetCore.Mvc;

namespace VoiceFlow.Api.Controllers;

[ApiController]
[Route("api/v1/health")]
public sealed class HealthController : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public IActionResult GetHealth() =>
        Ok(new { status = "healthy", timestamp = DateTime.UtcNow });
}
