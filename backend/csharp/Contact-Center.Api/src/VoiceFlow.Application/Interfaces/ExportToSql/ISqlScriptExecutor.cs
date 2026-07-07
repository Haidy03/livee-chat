using ExportToSql.Application.Models;
using ExportToSql.Domain.Execution;
using ExportToSql.Domain.Sql;

namespace ExportToSql.Application.Abstractions;

/// <summary>
/// Outbound port. The Application layer defines what it needs to run SQL;
/// the Infrastructure layer provides the MariaDB/MySQL implementation. This inversion
/// keeps the inner layers free of any database dependency.
/// </summary>
public interface ISqlScriptExecutor
{
    /// <summary>Executes the given statements against the target store.</summary>
    Task<ScriptOutcome> ExecuteAsync(
        IReadOnlyList<SqlStatement> statements,
        ExecutionSettings settings,
        CancellationToken cancellationToken = default);
}
