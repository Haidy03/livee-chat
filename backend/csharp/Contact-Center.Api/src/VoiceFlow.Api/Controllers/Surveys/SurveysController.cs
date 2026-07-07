using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Text;
using VoiceFlow.Application.Interfaces.Surveys;
using VoiceFlow.Contracts.Common;
using VoiceFlow.Contracts.Surveys;
using VoiceFlow.Core.Entities.Surveys;
using VoiceFlow.Core.Enums.Survey;
using VoiceFlow.Surveys.Application;


namespace VoiceFlow.Api.Controllers.Surveys;

[ApiController]
[Authorize]
[Route("api/v1/surveys")]
public class SurveysController : ControllerBase
{
    private readonly ISurveyService _svc;
    private readonly IValidator<SurveyCreateRequest> _createV;
    private readonly IValidator<SurveyUpdateRequest> _updateV;
    private readonly IValidator<SurveyWebhookPayload> _webhookV;

    public SurveysController(ISurveyService svc,
        IValidator<SurveyCreateRequest> createV,
        IValidator<SurveyUpdateRequest> updateV,
        IValidator<SurveyWebhookPayload> webhookV)
    {
        _svc = svc;
        _createV = createV;
        _updateV = updateV;
        _webhookV = webhookV;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<Survey>>>> List(
        [FromQuery] string? search, [FromQuery] SurveyStatus? status,
        [FromQuery] SurveyLanguage? language, CancellationToken ct)
    {
        var data = await _svc.ListAsync(search, status, language, ct);
        return Ok(ApiResponse<IReadOnlyList<Survey>>.Ok(data));
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ApiResponse<Survey>>> Get(string id, CancellationToken ct)
        => Ok(ApiResponse<Survey>.Ok(await _svc.GetAsync(id, ct)));

    [HttpPost]
    public async Task<ActionResult<ApiResponse<Survey>>> Create([FromBody] SurveyCreateRequest req, CancellationToken ct)
    {
        await _createV.ValidateAndThrowAsync(req, ct);
        var created = await _svc.CreateAsync(req, ct);
        return CreatedAtAction(nameof(Get), new { id = created.Id }, ApiResponse<Survey>.Ok(created));
    }

    [HttpPatch("{id}")]
    public async Task<ActionResult<ApiResponse<Survey>>> Update(string id, [FromBody] SurveyUpdateRequest req, CancellationToken ct)
    {
        await _updateV.ValidateAndThrowAsync(req, ct);
        return Ok(ApiResponse<Survey>.Ok(await _svc.UpdateAsync(id, req, ct)));
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(string id, CancellationToken ct)
    {
        await _svc.DeleteAsync(id, ct);
        return NoContent();
    }

    [HttpPatch("{id}/status")]
    public async Task<ActionResult<ApiResponse<Survey>>> SetStatus(string id, [FromBody] SetStatusRequest req, CancellationToken ct)
        => Ok(ApiResponse<Survey>.Ok(await _svc.SetStatusAsync(id, req.Status, ct)));

    [HttpPost("{id}/duplicate")]
    public async Task<ActionResult<ApiResponse<Survey>>> Duplicate(string id, CancellationToken ct)
    {
        var copy = await _svc.DuplicateAsync(id, ct);
        return CreatedAtAction(nameof(Get), new { id = copy.Id }, ApiResponse<Survey>.Ok(copy));
    }

    [HttpGet("{id}/responses")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<SurveyResponse>>>> Responses(
        string id, [FromQuery] int limit = 50,
        [FromQuery] DateTimeOffset? from = null, [FromQuery] DateTimeOffset? to = null,
        CancellationToken ct = default)
    {
        var data = await _svc.ListResponsesAsync(id, Math.Clamp(limit, 1, 500), from, to, ct);
        return Ok(ApiResponse<IReadOnlyList<SurveyResponse>>.Ok(data));
    }


    [AllowAnonymous]
    [HttpPost("webhook")]
    public async Task<ActionResult<ApiResponse<SurveyResponse>>> Webhook(
       [FromHeader(Name = "X-Survey-Signature")] string? signature,
       CancellationToken ct)
    {
        Request.EnableBuffering();
        string raw;
        using (var reader = new StreamReader(Request.Body, Encoding.UTF8, leaveOpen: true))
        {
            raw = await reader.ReadToEndAsync(ct);
            Request.Body.Position = 0;
        }

        SurveyWebhookPayload? payload;
        try
        {
            payload = System.Text.Json.JsonSerializer.Deserialize<SurveyWebhookPayload>(raw,
                new System.Text.Json.JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.SnakeCaseLower,
                    Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter(System.Text.Json.JsonNamingPolicy.SnakeCaseLower) },
                });
        }
        catch (System.Text.Json.JsonException ex)
        {
            return BadRequest(ApiResponse<SurveyResponse>.Fail($"Invalid JSON: {ex.Message}"));
        }

        if (payload is null)
            return BadRequest(ApiResponse<SurveyResponse>.Fail("Empty payload"));

        await _webhookV.ValidateAndThrowAsync(payload, ct);

        var result = await _svc.IngestWebhookAsync( payload, raw, signature, ct);
        var message = result.Duplicate ? "duplicate" : "accepted";
        return Ok(ApiResponse<SurveyResponse>.Ok(result.Response, message));
    }
}
