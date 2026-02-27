namespace RoslynNavigator.Models;

/// <summary>
/// Result of the snapshot command containing metadata about the generated snapshot.
/// </summary>
public class SnapshotCommandResult
{
    /// <summary>
    /// Indicates whether the snapshot was created successfully.
    /// </summary>
    public bool Success { get; set; } = true;

    /// <summary>
    /// Path to the solution that was snapshotted.
    /// </summary>
    public string SolutionPath { get; set; } = string.Empty;

    /// <summary>
    /// Path to the generated snapshot database file.
    /// </summary>
    public string DbPath { get; set; } = string.Empty;

    /// <summary>
    /// Number of classes persisted in the snapshot.
    /// </summary>
    public int ClassCount { get; set; }

    /// <summary>
    /// Number of methods persisted in the snapshot.
    /// </summary>
    public int MethodCount { get; set; }

    /// <summary>
    /// Number of call relationships persisted in the snapshot.
    /// </summary>
    public int CallCount { get; set; }

    /// <summary>
    /// Number of dependency relationships persisted in the snapshot.
    /// </summary>
    public int DependencyCount { get; set; }

    /// <summary>
    /// Number of annotations persisted in the snapshot.
    /// </summary>
    public int AnnotationCount { get; set; }

    /// <summary>
    /// Elapsed time in milliseconds for the snapshot operation.
    /// </summary>
    public long ElapsedMs { get; set; }

    /// <summary>
    /// Timestamp when the snapshot was generated (ISO 8601).
    /// </summary>
    public string GeneratedAt { get; set; } = string.Empty;

    /// <summary>
    /// Schema version used for the snapshot.
    /// </summary>
    public int SchemaVersion { get; set; }
}
