using ExportToSql.Application.Abstractions;
using ExportToSql.Application.Models;
using ExportToSql.Domain.Exceptions;
using ExportToSql.Domain.Execution;
using ExportToSql.Domain.Sql;
using Microsoft.Extensions.Options;
using MySqlConnector;
using VoiceFlow.Infrastructure.Options;

namespace ExportToSql.Infrastructure.Persistence;

/// <summary>
/// Executes SQL statements against MySQL/MariaDB. When transactional, the whole
/// script commits together or rolls back on the first failure.
/// </summary>
public sealed class MySqlScriptExecutor : ISqlScriptExecutor
{
    private readonly string _connectionString;

    public MySqlScriptExecutor(IOptions<MariaDbOptions> options)
    {
        var opt = options.Value;
        if (string.IsNullOrWhiteSpace(opt.ConnectionString))
            throw new InvalidOperationException("MariaDB:ConnectionString is not configured.");

        var builder = new MySqlConnectionStringBuilder(opt.ConnectionString);
        if (string.IsNullOrWhiteSpace(builder.Database) && !string.IsNullOrWhiteSpace(opt.AsteriskDbName))
            builder.Database = opt.AsteriskDbName;

        _connectionString = builder.ConnectionString;
    }

    public async Task<ScriptOutcome> ExecuteAsync(
        IReadOnlyList<SqlStatement> statements,
        ExecutionSettings settings,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(statements);
        ArgumentNullException.ThrowIfNull(settings);

        var results = new List<StatementOutcome>(statements.Count);

        await using var connection = new MySqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        MySqlTransaction? transaction = settings.UseTransaction
            ? await connection.BeginTransactionAsync(cancellationToken)
            : null;

        try
        {
            foreach (var statement in statements)
            {
                try
                {
                    await using var command = new MySqlCommand(statement.Text, connection, transaction);
                    if (settings.CommandTimeoutSeconds is { } timeout)
                        command.CommandTimeout = timeout;

                    var rowsAffected = await command.ExecuteNonQueryAsync(cancellationToken);
                    results.Add(new StatementOutcome(statement.Index, statement.Text, rowsAffected, null));
                }
                catch (MySqlException ex) when (settings.ContinueOnError)
                {
                    // Best-effort mode: record the failure and move on.
                    results.Add(new StatementOutcome(statement.Index, statement.Text, 0, ex.Message));
                }
                catch (MySqlException ex)
                {
                    // Transactional mode: abort the whole script.
                    throw new ScriptExecutionException(
                        statement.Index, statement.Text,
                        $"Statement #{statement.Index} failed: {ex.Message}", ex);
                }
            }

            if (transaction is not null)
                await transaction.CommitAsync(cancellationToken);

            return new ScriptOutcome(results);
        }
        catch
        {
            if (transaction is not null)
            {
                try { await transaction.RollbackAsync(cancellationToken); }
                catch { /* connection may already be unusable; surface the original error */ }
            }
            throw;
        }
        finally
        {
            if (transaction is not null)
                await transaction.DisposeAsync();
        }
    }
}
