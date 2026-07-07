using ExportToSql.Application.Abstractions;
using ExportToSql.Application.Contracts;
using ExportToSql.Application.Models;
using ExportToSql.Domain.Sql;

namespace ExportToSql.Application.Services;

/// <summary>
/// Implements the export use case: validate the request, apply token
/// substitutions, split the script with the domain splitter, run it via the
/// executor port, and map the domain outcome back to a response DTO.
/// </summary>
public sealed class ExportToSqlService : IExportToSqlService
{
    private const int PreviewLength = 120;

    private readonly ISqlScriptExecutor _executor;

    public ExportToSqlService(ISqlScriptExecutor executor)
        => _executor = executor ?? throw new ArgumentNullException(nameof(executor));

    public async Task<ExportToSqlResponse> ExportAsync(
        ExportToSqlRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (request.UseTransaction && request.ContinueOnError)
            throw new ArgumentException(
                "ContinueOnError requires UseTransaction = false — a failed statement aborts a transaction.");

        var script = request.Script;
        if (request.Tokens is { Count: > 0 })
            foreach (var (token, value) in request.Tokens)
                script = script.Replace(token, value);

        var statements = SqlScriptSplitter.Split(script);

        var settings = new ExecutionSettings
        {
            UseTransaction = request.UseTransaction,
            ContinueOnError = request.ContinueOnError,
            CommandTimeoutSeconds = request.CommandTimeoutSeconds,
        };

        var outcome = await _executor.ExecuteAsync(statements, settings, cancellationToken);

        var statementDtos = outcome.Statements
            .Select(s => new StatementResultDto(s.Index, s.Succeeded, s.RowsAffected, s.Error, Preview(s.Sql)))
            .ToList();

        return new ExportToSqlResponse(
            outcome.Succeeded, outcome.StatementCount, outcome.TotalRowsAffected, statementDtos);
    }

    private static string Preview(string sql)
    {
        var oneLine = string.Join(' ',
            sql.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));
        return oneLine.Length <= PreviewLength ? oneLine : oneLine[..PreviewLength] + "…";
    }
}
