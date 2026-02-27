using Microsoft.Data.Sqlite;
using RoslynNavigator.Models;
using RoslynNavigator.Snapshot.Services;

namespace RoslynNavigator.Commands;

/// <summary>
/// Orchestrates snapshot generation: path resolution, schema initialization, and extraction.
/// </summary>
public class SnapshotCommand
{
    private readonly SnapshotPathService _pathService;
    private readonly SnapshotSchemaService _schemaService;
    private readonly SnapshotExtractorService _extractorService;

    public SnapshotCommand() : this(
        new SnapshotPathService(),
        new SnapshotSchemaService(),
        new SnapshotExtractorService())
    {
    }

    public SnapshotCommand(
        SnapshotPathService pathService,
        SnapshotSchemaService schemaService,
        SnapshotExtractorService extractorService)
    {
        _pathService = pathService;
        _schemaService = schemaService;
        _extractorService = extractorService;
    }

    /// <summary>
    /// Executes the snapshot command with explicit or default DB path.
    /// </summary>
    /// <param name="solutionPath">Path to the solution file.</param>
    /// <param name="dbPath">Optional explicit path for the snapshot DB. If null, uses default path.</param>
    /// <returns>Structured result with counts and metadata.</returns>
    public async Task<SnapshotCommandResult> ExecuteAsync(string solutionPath, string? dbPath)
    {
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        // Resolve output path
        var resolvedDbPath = string.IsNullOrEmpty(dbPath)
            ? _pathService.ResolveDefaultPath(solutionPath)
            : dbPath;

        // Normalize paths
        var normalizedSolution = _pathService.NormalizePath(solutionPath);
        var normalizedDbPath = _pathService.NormalizePath(resolvedDbPath);

        // Initialize schema (creates DB with tables)
        _schemaService.InitializeDatabase(normalizedDbPath, normalizedSolution);

        // Run extraction
        await _extractorService.ExtractSolutionAsync(normalizedDbPath, normalizedSolution);

        stopwatch.Stop();

        // Get entity counts
        var counts = GetEntityCounts(normalizedDbPath);
        var meta = _schemaService.GetSnapshotMeta(normalizedDbPath);

        return new SnapshotCommandResult
        {
            Success = true,
            SolutionPath = normalizedSolution,
            DbPath = normalizedDbPath,
            ClassCount = counts.ClassCount,
            MethodCount = counts.MethodCount,
            CallCount = counts.CallCount,
            DependencyCount = counts.DependencyCount,
            AnnotationCount = counts.AnnotationCount,
            ElapsedMs = stopwatch.ElapsedMilliseconds,
            GeneratedAt = meta?.GeneratedAt ?? DateTime.UtcNow.ToString("O"),
            SchemaVersion = meta?.SchemaVersion ?? 1
        };
    }

    private (int ClassCount, int MethodCount, int CallCount, int DependencyCount, int AnnotationCount) GetEntityCounts(string dbPath)
    {
        using var connection = new SqliteConnection($"Data Source={dbPath}");
        connection.Open();

        var classCount = ExecuteCountQuery(connection, "SELECT COUNT(*) FROM classes");
        var methodCount = ExecuteCountQuery(connection, "SELECT COUNT(*) FROM methods");
        var callCount = ExecuteCountQuery(connection, "SELECT COUNT(*) FROM calls");
        var dependencyCount = ExecuteCountQuery(connection, "SELECT COUNT(*) FROM dependencies");
        var annotationCount = ExecuteCountQuery(connection, "SELECT COUNT(*) FROM annotations");

        return (classCount, methodCount, callCount, dependencyCount, annotationCount);
    }

    private int ExecuteCountQuery(SqliteConnection connection, string sql)
    {
        using var command = connection.CreateCommand();
        command.CommandText = sql;
        return Convert.ToInt32(command.ExecuteScalar());
    }
}
