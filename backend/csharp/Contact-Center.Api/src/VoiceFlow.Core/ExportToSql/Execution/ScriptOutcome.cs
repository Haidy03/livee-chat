namespace ExportToSql.Domain.Execution;

/// <summary>Outcome of executing a single statement.</summary>
/// <param name="Index">Position of the statement within the script.</param>
/// <param name="Sql">The statement text that was executed.</param>
/// <param name="RowsAffected">Rows affected, or 0 when the statement failed.</param>
/// <param name="Error">Error message when the statement failed, otherwise null.</param>
public sealed record StatementOutcome(int Index, string Sql, int RowsAffected, string? Error)
{
    public bool Succeeded => Error is null;
}

/// <summary>Aggregate outcome of executing a whole script.</summary>
public sealed record ScriptOutcome(IReadOnlyList<StatementOutcome> Statements)
{
    public bool Succeeded => Statements.All(s => s.Succeeded);
    public int StatementCount => Statements.Count;
    public int TotalRowsAffected => Statements.Sum(s => s.RowsAffected);
}
