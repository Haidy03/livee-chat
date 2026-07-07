using CtiBackend.Models.Responses;
using CtiBackend.Services.Ami;
using Microsoft.AspNetCore.Mvc;

namespace CtiBackend.Controllers;

[ApiController]
[Route("api/cti")]
public sealed class HealthController : ControllerBase
{
    private readonly AmiConnectionStatus _ami;
    public HealthController(AmiConnectionStatus ami) => _ami = ami;

    [HttpGet("health")]
    public ActionResult<ApiResponse<object>> Health() =>
        Ok(ApiResponse<object>.Ok(new
        {
            status = "ok",
            ami = _ami.Snapshot(),
            timeUtc = DateTime.UtcNow,
        }));

    [HttpGet("ami/status")]
    public ActionResult<ApiResponse<object>> AmiStatus() =>
        Ok(ApiResponse<object>.Ok(_ami.Snapshot()));
}
