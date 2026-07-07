namespace ExportToSql.Domain.Sql;

/// <summary>A single executable SQL statement extracted from a script.</summary>
/// <param name="Index">Zero-based position of the statement within the script.</param>
/// <param name="Text">The statement text, trimmed and comment-stripped.</param>
public sealed record SqlStatement(int Index, string Text);
