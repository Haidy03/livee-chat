namespace ExportToSql.Domain.Exceptions;

/// <summary>
/// Raised when a statement fails. In transactional mode the whole script is
/// rolled back before this is thrown.
/// </summary>
public sealed class ScriptExecutionException : Exception
{
    public ScriptExecutionException(
        int failedStatementIndex, string failedStatement, string message, Exception? innerException = null)
        : base(message, innerException)
    {
        FailedStatementIndex = failedStatementIndex;
        FailedStatement = failedStatement;
    }

    /// <summary>Zero-based index of the statement that failed.</summary>
    public int FailedStatementIndex { get; }

    /// <summary>The text of the statement that failed.</summary>
    public string FailedStatement { get; }
}
