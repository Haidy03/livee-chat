using ExportToSql.Application.Abstractions;
using ExportToSql.Application.Contracts;
using ExportToSql.Domain.Exceptions;
using Microsoft.AspNetCore.Mvc;

namespace ExportToSql.Api.Controllers.V1;

/// <summary>Endpoint for executing a SQL export script against the configured database.</summary>
[ApiController]
[Route("api/v1/exporttosql")]
[Produces("application/json")]
public sealed class ExportToSqlController : ControllerBase
{
    private readonly IExportToSqlService _exportService;
    private readonly ILogger<ExportToSqlController> _logger;

    public ExportToSqlController(
        IExportToSqlService exportService, ILogger<ExportToSqlController> logger)
    {
        _exportService = exportService;
        _logger = logger;
    }

    /// <summary>Executes the supplied SQL script and reports the per-statement results.</summary>
    /// <response code="200">The script ran (check <c>succeeded</c> for per-statement detail).</response>
    /// <response code="400">The request was invalid.</response>
    /// <response code="422">A statement failed and the transaction was rolled back.</response>
    [HttpPost]
    [ProducesResponseType(typeof(ExportToSqlResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> ExportToSql(
        [FromBody] ExportToSqlRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var response = await _exportService.ExportAsync(request, cancellationToken);
            return Ok(response);
        }
        catch (ArgumentException ex)
        {
            return Problem(
                title: "Invalid request",
                detail: ex.Message,
                statusCode: StatusCodes.Status400BadRequest);
        }
        catch (ScriptExecutionException ex)
        {
            _logger.LogWarning(ex, "Script execution failed at statement #{Index}", ex.FailedStatementIndex);
            return Problem(
                title: "Script execution failed",
                detail: ex.Message,
                statusCode: StatusCodes.Status422UnprocessableEntity);
        }
    }
}
