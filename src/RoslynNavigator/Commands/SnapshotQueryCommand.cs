using Microsoft.Data.Sqlite;
using RoslynNavigator.Models;
using RoslynNavigator.Snapshot.Services;
using RoslynNavigator.Rules.Services;

namespace RoslynNavigator.Commands;

/// <summary>
/// Executes arbitrary SQL queries against a snapshot database.
/// </summary>
public class SnapshotQueryCommand
{
    private readonly SqlReadOnlyGuard _guard;
    private readonly SnapshotPathService _pathService;

    public SnapshotQueryCommand() : this(new SqlReadOnlyGuard(), new SnapshotPathService())
    {
    }

    public SnapshotQueryCommand(SqlReadOnlyGuard guard, SnapshotPathService pathService)
    {
        _guard = guard;
        _pathService = pathService;
    }

    /// <summary>
    /// Exec against autes a query snapshot database.
    /// </summary>
    /// <param name="sql">The SQL query to execute.</param>
    /// <param name="dbPath">Path to the snapshot database (optional - will resolve to default if not provided).</param>
    /// <param name="solutionPath">Solution path for default DB resolution (required if dbPath not provided).</param>
    /// <returns>Query result with rows.</returns>
    public async Task<SnapshotQueryResult> ExecuteAsync(string sql, string? dbPath, string? solutionPath = null)
    {
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        var result = new SnapshotQueryResult
        {
            Sql = sql
        };

        try
        {
            // Resolve dbPath
            if (string.IsNullOrEmpty(dbPath))
            {
                if (string.IsNullOrEmpty(solutionPath))
                {
                    result.Success = false;
                    result.ErrorMessage = "Either --db or --solution must be provided. Run 'roslyn-nav snapshot --solution <path>' first to generate a snapshot, or specify --db explicitly.";
                    stopwatch.Stop();
                    result.ElapsedMs = stopwatch.ElapsedMilliseconds;
                    return result;
                }

                try
                {
                    dbPath = _pathService.ResolveDefaultPath(solutionPath);
                }
                catch (Exception ex)
                {
                    result.Success = false;
                    result.ErrorMessage = $"Could not resolve default snapshot path: {ex.Message}";
                    stopwatch.Stop();
                    result.ElapsedMs = stopwatch.ElapsedMilliseconds;
                    return result;
                }
            }

            result.DbPath = _pathService.NormalizePath(dbPath);

            // Validate dbPath exists
            if (!File.Exists(result.DbPath))
            {
                result.Success = false;
                result.ErrorMessage = $"Snapshot database not found: {result.DbPath}. Run 'roslyn-nav snapshot --solution <path>' first.";
                stopwatch.Stop();
                result.ElapsedMs = stopwatch.ElapsedMilliseconds;
                return result;
            }

            // Validate SQL is read-only using shared guard
            var validation = _guard.Validate(sql);
            if (!validation.IsValid)
            {
                result.Success = false;
                result.ErrorMessage = validation.Reason;
                stopwatch.Stop();
                result.ElapsedMs = stopwatch.ElapsedMilliseconds;
                return result;
            }

            // Execute query
            var connectionString = $"Data Source={result.DbPath}";
            using var connection = new SqliteConnection(connectionString);
            connection.Open();

            using var command = connection.CreateCommand();
            command.CommandText = sql;

            using var reader = command.ExecuteReader();

            // Read column names
            var columnNames = new string[reader.FieldCount];
            for (var i = 0; i < reader.FieldCount; i++)
            {
                columnNames[i] = reader.GetName(i);
            }

            // Read rows
            while (reader.Read())
            {
                var row = new Dictionary<string, object?>();
                for (var i = 0; i < reader.FieldCount; i++)
                {
                    var value = reader.IsDBNull(i) ? null : reader.GetValue(i);
                    row[columnNames[i]] = value;
                }
                result.Rows.Add(row);
            }

            result.RowCount = result.Rows.Count;
            result.Success = true;
        }
        catch (Exception ex)
        {
            result.Success = false;
            result.ErrorMessage = $"Query execution failed: {ex.Message}";
        }

        stopwatch.Stop();
        result.ElapsedMs = stopwatch.ElapsedMilliseconds;
        return result;
    }
}
