using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VoiceFlow.Application.Interfaces.Reports;
using VoiceFlow.Contracts.Accounts;
using VoiceFlow.Contracts.Common;
using VoiceFlow.Contracts.Reports;
using VoiceFlow.Core.Enums.Reports;

namespace VoiceFlow.Reports.Api.Controllers;

/// <summary>Reports management API.</summary>
[ApiController]
[Authorize]
[Route("api/v1/reports")]
[Produces("application/json")]
public sealed class ReportsController : ControllerBase
{
    private readonly IReportService _service;

    public ReportsController(IReportService service)
        => _service = service;

    /// <summary>List reports for the current tenant.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<PagedResponse<ReportResponse>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<PagedResponse<ReportResponse>>>> List(
        [FromQuery] string? search,
        [FromQuery] string? category,
        [FromQuery] string? status,
        [FromQuery] bool? starred,
        [FromQuery] string? ownerId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        [FromQuery] string? sort = null,
        CancellationToken ct = default)
    {
        var res = await _service.ListAsync(search, category, status, starred, ownerId, page, pageSize, sort, ct);

        if (res.IsFailure)
            return Ok(ApiResponse<PagedResponse<ReportResponse>>.Fail(res.Error.Description));

        return Ok(ApiResponse<PagedResponse<ReportResponse>>.Ok(res.Value));
    }

    /// <summary>Get a single report.</summary>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(ApiResponse<ReportResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<ReportResponse>>> Get(string id, CancellationToken ct)
    {
        var res = await _service.GetAsync(id, ct);

        if (res.IsFailure)
            return Ok(ApiResponse<ReportResponse>.Fail(res.Error.Description));

        return Ok(ApiResponse<ReportResponse>.Ok(res.Value));
    }

    /// <summary>Create a new report.</summary>
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<ReportResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<ReportResponse>>> Create(
        [FromBody] CreateReportRequest request,
        CancellationToken ct)
    {
        var res = await _service.CreateAsync(request, ct);

        if (res.IsFailure)
            return Ok(ApiResponse<ReportResponse>.Fail(res.Error.Description));

        return Ok(ApiResponse<ReportResponse>.Ok(res.Value));
    }

    /// <summary>Update fields on an existing report (partial).</summary>
    [HttpPatch("{id}")]
    [ProducesResponseType(typeof(ApiResponse<ReportResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<ReportResponse>>> Update(
        string id,
        [FromBody] UpdateReportRequest request,
        CancellationToken ct)
    {
        var res = await _service.UpdateAsync(id, request, ct);

        if (res.IsFailure)
            return Ok(ApiResponse<ReportResponse>.Fail(res.Error.Description));

        return Ok(ApiResponse<ReportResponse>.Ok(res.Value));
    }

    /// <summary>Delete a report.</summary>
    [HttpDelete("{id}")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<object>>> Delete(string id, CancellationToken ct)
    {
        var res = await _service.DeleteAsync(id, ct);

        if (res.IsFailure)
            return Ok(ApiResponse<object>.Fail(res.Error.Description));

        return NoContent();
    }

    /// <summary>Bulk update status (pause / activate / draft).</summary>
    [HttpPost("bulk-status")]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<ReportResponse>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<ReportResponse>>>> BulkStatus(
        [FromBody] BulkStatusRequest request,
        CancellationToken ct)
    {
        var res = await _service.BulkSetStatusAsync(request, ct);

        if (res.IsFailure)
            return Ok(ApiResponse<IReadOnlyList<ReportResponse>>.Fail(res.Error.Description));

        return Ok(ApiResponse<IReadOnlyList<ReportResponse>>.Ok(res.Value));
    }

    /// <summary>Run a report on demand.</summary>
    [HttpPost("{id}/run")]
    [ProducesResponseType(typeof(ApiResponse<ReportResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<ReportResponse>>> Run(string id, CancellationToken ct)
    {
        var res = await _service.RunAsync(id, ct);

        if (res.IsFailure)
            return Ok(ApiResponse<ReportResponse>.Fail(res.Error.Description));

        return  (ApiResponse<ReportResponse>.Ok(res.Value));
    }

    [HttpGet("{id}/runs")]
    [ProducesResponseType(typeof(ApiResponse<PagedResponse<ReportRunResponse>>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<PagedResponse<ReportRunResponse>>>> Runs(
     string id,
     [FromQuery] int page = 1,
     [FromQuery] int pageSize = 20,
     CancellationToken ct = default)
    {
        var res = await _service.ListRunsAsync(id, page, pageSize, ct);

        if (res.IsFailure)
            return Ok(ApiResponse<PagedResponse<ReportRunResponse>>.Fail(res.Error.Description));

        return Ok(ApiResponse<PagedResponse<ReportRunResponse>>.Ok(res.Value));
    }

    /// <summary>Get the produced result (columns, rows, summary) for a specific run.</summary>
    [HttpGet("{id}/runs/{runId}/result")]
    [ProducesResponseType(typeof(ApiResponse<ReportResultResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<ReportResultResponse>>> RunResult(
        string id,
        string runId,
        CancellationToken ct)
    {
        var res = await _service.GetRunResultAsync(id, runId, ct);

        if (res.IsFailure)
            return Ok(ApiResponse<ReportResultResponse>.Fail(res.Error.Description));

        return Ok(ApiResponse<ReportResultResponse>.Ok(res.Value));
    }

    /// <summary>Get the most recent succeeded result for the report.</summary>
    [HttpGet("{id}/result/latest")]
    [ProducesResponseType(typeof(ApiResponse<ReportResultResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<ReportResultResponse>>> LatestResult(
        string id,
        CancellationToken ct)
    {
        var res = await _service.GetLatestResultAsync(id, ct);

        if (res.IsFailure)
            return Ok(ApiResponse<ReportResultResponse>.Fail(res.Error.Description));

        return Ok(ApiResponse<ReportResultResponse>.Ok(res.Value));
    }

    /// <summary>Download a run result rendered as a file (csv, xlsx, html or pdf).</summary>
    [HttpGet("{id}/runs/{runId}/export")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ExportRunResult(
        string id,
        string runId,
        [FromQuery] string format,
        [FromQuery] string? lang,
        CancellationToken ct)
    {
        if (!Enum.TryParse<ExportFormat>(format, ignoreCase: true, out var parsed))
            return BadRequest(ApiResponse<object>.Fail($"Unknown export format '{format}'."));

        var res = await _service.ExportRunResultAsync(id, runId, parsed, lang ?? "en", ct);
        if (res.IsFailure)
            return NotFound(ApiResponse<object>.Fail(res.Error.Description));

        return File(res.Value.Content, res.Value.ContentType, res.Value.FileName);
    }

    /// <summary>List all supported data sources with their fields and metrics.</summary>
    [HttpGet("data-sources")]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<DataSourceMetadataResponse>>), StatusCodes.Status200OK)]
    public ActionResult<ApiResponse<IReadOnlyList<DataSourceMetadataResponse>>> ListDataSources()
    {
        var res = _service.ListDataSources();
        return Ok(ApiResponse<IReadOnlyList<DataSourceMetadataResponse>>.Ok(res.Value));
    }

    /// <summary>Return the fields and metrics available on a single data source.</summary>
    [HttpGet("data-sources/{key}/metadata")]
    [ProducesResponseType(typeof(ApiResponse<DataSourceMetadataResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public ActionResult<ApiResponse<DataSourceMetadataResponse>> DataSourceMetadata(string key)
    {
        var res = _service.GetDataSourceMetadata(key);
        if (res.IsFailure)
            return Ok(ApiResponse<DataSourceMetadataResponse>.Fail(res.Error.Description));
        return Ok(ApiResponse<DataSourceMetadataResponse>.Ok(res.Value));
    }
}
