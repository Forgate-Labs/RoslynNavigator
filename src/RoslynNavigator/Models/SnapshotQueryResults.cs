using System.Text.Json.Serialization;

namespace RoslynNavigator.Models;

/// <summary>
/// Result of a snapshot query command execution.
/// </summary>
public class SnapshotQueryResult
{
    /// <summary>
    /// Whether the query succeeded.
    /// </summary>
    [JsonPropertyName("success")]
    public bool Success { get; set; }

    /// <summary>
    /// Path to the snapshot database that was queried.
    /// </summary>
    [JsonPropertyName("dbPath")]
    public string DbPath { get; set; } = string.Empty;

    /// <summary>
    /// The SQL query that was executed.
    /// </summary>
    [JsonPropertyName("sql")]
    public string Sql { get; set; } = string.Empty;

    /// <summary>
    /// Number of rows returned.
    /// </summary>
    [JsonPropertyName("rowCount")]
    public int RowCount { get; set; }

    /// <parameter name="Rows">
    /// Query result rows as array of objects.
    /// Each row is a dictionary with column names as keys.
    /// </parameter>
    [JsonPropertyName("rows")]
    public List<Dictionary<string, object?>> Rows { get; set; } = new();

    /// <summary>
    /// Error message if the query failed.
    /// </summary>
    [JsonPropertyName("errorMessage")]
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Time elapsed in milliseconds.
    /// </summary>
    [JsonPropertyName("elapsedMs")]
    public long ElapsedMs { get; set; }
}
