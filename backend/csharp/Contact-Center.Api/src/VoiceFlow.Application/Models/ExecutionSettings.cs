namespace ExportToSql.Application.Models;

/// <summary>How a script should be executed. Passed from the use case to the executor port.</summary>
public sealed class ExecutionSettings
{
    /// <summary>Run the whole script in one transaction (commit all or roll back all).</summary>
    public bool UseTransaction { get; init; } = true;

    /// <summary>Keep going after a failing statement. Only valid when not transactional.</summary>
    public bool ContinueOnError { get; init; }

    /// <summary>Per-statement command timeout in seconds; null uses the driver default.</summary>
    public int? CommandTimeoutSeconds { get; init; }
}
