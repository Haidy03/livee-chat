using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VoiceFlow.Application.Interfaces.FreeSwitch;
using VoiceFlow.Contracts.Common;
using VoiceFlow.Contracts.FreeSwitch;
using VoiceFlow.Domain.FreeSwitch;

namespace VoiceFlow.Reports.Api.Controllers.FreeSwitch;

/// <summary>FreeSWITCH dialplan document ingestion.</summary>
[ApiController]
[Authorize]
[Route("api/v1/freeswitch")]
[Produces("application/json")]
public sealed class FreeswitchDialplanController : ControllerBase
{
    private readonly IDialplanDocumentService _service;

    public FreeswitchDialplanController(IDialplanDocumentService service) => _service = service;

    /// <summary>Bulk upsert FreeSWITCH dialplan documents (by id).</summary>
    [HttpPost("dialplan-documents")]
    [ProducesResponseType(typeof(ApiResponse<PushDialplanDocumentsResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ApiResponse<PushDialplanDocumentsResponse>>> Push(
     [FromBody] PushDialplanDocumentsRequest request,
     CancellationToken ct)
    {
        var res = await _service.PushAsync(request, ct);

        if (res.IsFailure)
            return Ok(ApiResponse<PushDialplanDocumentsResponse>.Fail(res.Error.Description));

        return ApiResponse<PushDialplanDocumentsResponse>.Ok(res.Value);
    }
}
