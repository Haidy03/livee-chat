using System.Text;

namespace ExportToSql.Domain.Sql;

/// <summary>
/// Pure domain service that splits a multi-statement SQL script into individual
/// <see cref="SqlStatement"/> values.
///
/// It is aware of single-quoted strings, double-quoted strings and backtick
/// identifiers (including <c>''</c> / <c>""</c> doubling and backslash escapes),
/// of <c>-- </c> / <c>#</c> / <c>/* */</c> comments (executable <c>/*! */</c>
/// comments are preserved), and of the <c>DELIMITER</c> directive. A <c>;</c>
/// inside a string or comment therefore never ends a statement incorrectly.
/// </summary>
public static class SqlScriptSplitter
{
    /// <summary>Splits <paramref name="script"/> into ordered, trimmed statements.</summary>
    public static IReadOnlyList<SqlStatement> Split(string script, string initialDelimiter = ";")
    {
        ArgumentNullException.ThrowIfNull(script);
        if (string.IsNullOrEmpty(initialDelimiter))
            throw new ArgumentException("Delimiter must not be empty.", nameof(initialDelimiter));

        var statements = new List<SqlStatement>();
        var sb = new StringBuilder();
        var delimiter = initialDelimiter;
        var seenContent = false; // true once the current statement has a non-whitespace char
        int i = 0, n = script.Length;

        void Emit()
        {
            var text = sb.ToString().Trim();
            if (text.Length > 0)
                statements.Add(new SqlStatement(statements.Count, text));
            sb.Clear();
            seenContent = false;
        }

        while (i < n)
        {
            var c = script[i];

            // DELIMITER directive — only valid at the start of a statement.
            if (!seenContent && MatchesKeyword(script, i, "DELIMITER"))
            {
                i += "DELIMITER".Length;
                while (i < n && (script[i] == ' ' || script[i] == '\t')) i++;
                var start = i;
                while (i < n && !char.IsWhiteSpace(script[i])) i++;
                if (i > start) delimiter = script[start..i];
                while (i < n && script[i] != '\n') i++;
                sb.Clear();
                seenContent = false;
                continue;
            }

            // Line comments: "-- " (dashes + whitespace) or "#" — dropped.
            if (c == '#' ||
                (c == '-' && i + 1 < n && script[i + 1] == '-' &&
                 (i + 2 >= n || char.IsWhiteSpace(script[i + 2]))))
            {
                while (i < n && script[i] != '\n') i++;
                continue;
            }

            // Block comments. A "/*! ... */" executable comment is kept; a plain one is dropped.
            if (c == '/' && i + 1 < n && script[i + 1] == '*')
            {
                var executable = i + 2 < n && script[i + 2] == '!';
                i += 2;
                while (i < n && !(script[i] == '*' && i + 1 < n && script[i + 1] == '/'))
                {
                    if (executable) { sb.Append(script[i]); seenContent = true; }
                    i++;
                }
                if (executable && i < n) sb.Append("*/");
                i = Math.Min(i + 2, n);
                continue;
            }

            // Quoted strings ('...', "...") and quoted identifiers (`...`).
            if (c is '\'' or '"' or '`')
            {
                sb.Append(c);
                seenContent = true;
                i++;
                while (i < n)
                {
                    var d = script[i];

                    // Backslash escapes apply inside '' and "" but not `` ``.
                    if (d == '\\' && c != '`' && i + 1 < n)
                    {
                        sb.Append(d).Append(script[i + 1]);
                        i += 2;
                        continue;
                    }

                    sb.Append(d);
                    if (d == c)
                    {
                        if (i + 1 < n && script[i + 1] == c) // doubled-quote escape
                        {
                            sb.Append(c);
                            i += 2;
                            continue;
                        }
                        i++;
                        break;
                    }
                    i++;
                }
                continue;
            }

            // Statement terminator.
            if (MatchesAt(script, i, delimiter))
            {
                Emit();
                i += delimiter.Length;
                continue;
            }

            sb.Append(c);
            if (!char.IsWhiteSpace(c)) seenContent = true;
            i++;
        }

        Emit(); // trailing statement without a delimiter
        return statements;
    }

    private static bool MatchesAt(string sql, int index, string token)
        => index + token.Length <= sql.Length &&
           string.CompareOrdinal(sql, index, token, 0, token.Length) == 0;

    private static bool MatchesKeyword(string sql, int index, string keyword)
    {
        if (index + keyword.Length > sql.Length) return false;
        if (string.Compare(sql, index, keyword, 0, keyword.Length,
                StringComparison.OrdinalIgnoreCase) != 0) return false;
        var after = index + keyword.Length;
        return after >= sql.Length || char.IsWhiteSpace(sql[after]);
    }
}
