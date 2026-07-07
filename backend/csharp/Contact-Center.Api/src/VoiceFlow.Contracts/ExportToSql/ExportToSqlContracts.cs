using System.ComponentModel.DataAnnotations;

namespace ExportToSql.Application.Contracts;

/// <summary>Request body for <c>POST /api/v1/exporttosql</c>.</summary>
public sealed class ExportToSqlRequest
{
    /// <summary>The raw SQL script — one or more delimiter-separated statements.</summary>
    [Required(AllowEmptyStrings = false)]
    public string Script { get; init; } = string.Empty;

    /// <summary>Run the whole script in a single transaction. Default: true.</summary>
    public bool UseTransaction { get; init; } = true;

    /// <summary>Keep going after a failing statement. Requires <see cref="UseTransaction"/> = false.</summary>
    public bool ContinueOnError { get; init; }

    /// <summary>Optional per-statement command timeout, in seconds (1–3600).</summary>
    [Range(1, 3600)]
    public int? CommandTimeoutSeconds { get; init; }

    /// <summary>
    /// Optional literal text substitutions applied to the script before it runs
    /// (e.g. filling a <c>&lt;CHANGE_ME&gt;</c> placeholder). Each key is a plain
    /// global text replace.
    /// </summary>
    public IReadOnlyDictionary<string, string>? Tokens { get; init; }
}

/// <summary>Per-statement outcome returned to the caller.</summary>
public sealed record StatementResultDto(
    int Index, bool Succeeded, int RowsAffected, string? Error, string SqlPreview);

/// <summary>Response body for <c>POST /api/v1/exporttosql</c>.</summary>
public sealed record ExportToSqlResponse(
    bool Succeeded,
    int StatementCount,
    int TotalRowsAffected,
    IReadOnlyList<StatementResultDto> Statements);
