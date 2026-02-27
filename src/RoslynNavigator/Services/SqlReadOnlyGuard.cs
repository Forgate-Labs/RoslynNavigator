using System.Text.RegularExpressions;

namespace RoslynNavigator.Services;

/// <summary>
/// Validates whether SQL is read-only and safe to run against snapshot databases.
/// </summary>
public class SqlReadOnlyGuard
{
    // Keywords that would modify data or schema
    private static readonly HashSet<string> MutatingKeywords = new(StringComparer.OrdinalIgnoreCase)
    {
        "INSERT", "UPDATE", "DELETE", "REPLACE",
        "CREATE", "DROP", "ALTER", "TRUNCATE",
        "PRAGMA", "ATTACH", "DETACH", "VACUUM"
    };

    // Pattern to detect statement separators followed by more statements
    private static readonly Regex StatementSeparatorPattern = new(
        @";\s*\S+",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    /// <summary>
    /// Validates whether the given SQL is read-only and safe to execute.
    /// </summary>
    /// <param name="sql">The SQL query to validate.</param>
    /// <returns>A validation result containing isValid flag and reason if invalid.</returns>
    public SqlReadOnlyValidationResult Validate(string sql)
    {
        if (string.IsNullOrWhiteSpace(sql))
        {
            return new SqlReadOnlyValidationResult
            {
                IsValid = false,
                Reason = "SQL cannot be empty or whitespace"
            };
        }

        var trimmedSql = sql.Trim();

        // Check for statement separators with additional content
        // This detects multi-statement queries like "SELECT 1; SELECT 2"
        if (StatementSeparatorPattern.IsMatch(trimmedSql))
        {
            return new SqlReadOnlyValidationResult
            {
                IsValid = false,
                Reason = "Multiple statements detected. Only single queries are allowed."
            };
        }

        // Extract the first meaningful token (skip whitespace and comments)
        var firstToken = ExtractFirstKeyword(trimmedSql);

        if (firstToken == null)
        {
            return new SqlReadOnlyValidationResult
            {
                IsValid = false,
                Reason = "Unable to determine SQL query type"
            };
        }

        // Only allow SELECT and WITH (CTEs) as read-only
        if (firstToken.Equals("SELECT", StringComparison.OrdinalIgnoreCase) ||
            firstToken.Equals("WITH", StringComparison.OrdinalIgnoreCase))
        {
            return new SqlReadOnlyValidationResult
            {
                IsValid = true,
                Reason = null
            };
        }

        // Check for mutating keywords
        if (MutatingKeywords.Contains(firstToken))
        {
            return new SqlReadOnlyValidationResult
            {
                IsValid = false,
                Reason = $"Mutating keyword '{firstToken}' is not allowed. Only SELECT queries are permitted."
            };
        }

        // Unknown query type - reject to be safe
        return new SqlReadOnlyValidationResult
        {
            IsValid = false,
            Reason = $"Query type '{firstToken}' is not allowed. Only SELECT queries are permitted."
        };
    }

    /// <summary>
    /// Extracts the first SQL keyword from the query, skipping whitespace and comments.
    /// </summary>
    private static string? ExtractFirstKeyword(string sql)
    {
        var i = 0;
        var length = sql.Length;

        while (i < length)
        {
            // Skip whitespace
            while (i < length && char.IsWhiteSpace(sql[i]))
            {
                i++;
            }

            if (i >= length)
                return null;

            // Check for single-line comment
            if (i + 1 < length && sql[i] == '-' && sql[i + 1] == '-')
            {
                // Skip to end of line
                while (i < length && sql[i] != '\n')
                {
                    i++;
                }
                continue;
            }

            // Check for multi-line comment
            if (i + 1 < length && sql[i] == '/' && sql[i + 1] == '*')
            {
                i += 2;
                while (i + 1 < length && !(sql[i] == '*' && sql[i + 1] == '/'))
                {
                    i++;
                }
                i += 2; // Skip closing */
                continue;
            }

            // Found start of actual content - extract keyword
            var start = i;
            while (i < length && !char.IsWhiteSpace(sql[i]) && sql[i] != '(' && sql[i] != ';')
            {
                i++;
            }

            if (start < i)
            {
                return sql.Substring(start, i - start);
            }

            break;
        }

        return null;
    }
}

/// <summary>
/// Result of SQL read-only validation.
/// </summary>
public class SqlReadOnlyValidationResult
{
    /// <summary>
    /// Whether the SQL is read-only and safe to execute.
    /// </summary>
    public bool IsValid { get; set; }

    /// <summary>
    /// If not valid, the reason for rejection.
    /// </summary>
    public string? Reason { get; set; }
}
