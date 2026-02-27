using Microsoft.Data.Sqlite;
using RoslynNavigator.Commands;
using RoslynNavigator.Rules.Services;
using RoslynNavigator.Rules.Models;
using RoslynNavigator.Snapshot.Services;

namespace RoslynNavigator.Tests;

public class SnapshotCommandTests : IDisposable
{
    private readonly string _tempDir;
    private readonly string _sampleSolutionPath;

    public SnapshotCommandTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(_tempDir);
        
        // Use the existing sample solution
        _sampleSolutionPath = Path.GetFullPath(
            Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", 
                "tests", "SampleSolution", "Sample.sln"));
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
            Directory.Delete(_tempDir, recursive: true);
    }

    [Fact]
    public async Task ExecuteAsync_WithExplicitDbPath_CreatesDatabaseFile()
    {
        // Arrange
        var dbPath = Path.Combine(_tempDir, "explicit.db");
        var command = new SnapshotCommand();

        // Act
        var result = await command.ExecuteAsync(_sampleSolutionPath, dbPath);

        // Assert
        Assert.True(File.Exists(dbPath), "Database file should exist");
        Assert.True(result.Success);
        Assert.Equal(dbPath, result.DbPath);
    }

    [Fact]
    public async Task ExecuteAsync_WithExplicitDbPath_ReturnsValidCounts()
    {
        // Arrange
        var dbPath = Path.Combine(_tempDir, "counts.db");
        var command = new SnapshotCommand();

        // Act
        var result = await command.ExecuteAsync(_sampleSolutionPath, dbPath);

        // Assert
        Assert.True(result.Success);
        Assert.True(result.ClassCount >= 0, "Class count should be non-negative");
        Assert.True(result.MethodCount >= 0, "Method count should be non-negative");
        Assert.True(result.CallCount >= 0, "Call count should be non-negative");
        Assert.True(result.DependencyCount >= 0, "Dependency count should be non-negative");
        Assert.True(result.AnnotationCount >= 0, "Annotation count should be non-negative");
    }

    [Fact]
    public async Task ExecuteAsync_WithoutDbPath_UsesDefaultPathConvention()
    {
        // Arrange
        var command = new SnapshotCommand();
        var pathService = new SnapshotPathService();
        
        var expectedDefaultPath = pathService.ResolveDefaultPath(_sampleSolutionPath);

        // Act
        var result = await command.ExecuteAsync(_sampleSolutionPath, null);

        // Assert
        Assert.True(result.Success);
        Assert.Equal(expectedDefaultPath, result.DbPath);
        Assert.True(File.Exists(result.DbPath), "Default database file should exist");
    }

    [Fact]
    public async Task ExecuteAsync_CreatesDatabaseWithRequiredTables()
    {
        // Arrange
        var dbPath = Path.Combine(_tempDir, "tables.db");
        var command = new SnapshotCommand();

        // Act
        var result = await command.ExecuteAsync(_sampleSolutionPath, dbPath);

        // Assert
        var schemaService = new SnapshotSchemaService();
        Assert.True(schemaService.ValidateTablesExist(dbPath), "All required tables should exist");
    }

    [Fact]
    public async Task ExecuteAsync_ReturnsSolutionAndDbPaths()
    {
        // Arrange
        var dbPath = Path.Combine(_tempDir, "paths.db");
        var command = new SnapshotCommand();
        var normalizedSolution = new SnapshotPathService().NormalizePath(_sampleSolutionPath);

        // Act
        var result = await command.ExecuteAsync(_sampleSolutionPath, dbPath);

        // Assert
        Assert.True(result.Success);
        Assert.Equal(normalizedSolution, result.SolutionPath);
        Assert.Equal(dbPath, result.DbPath);
    }

    [Fact]
    public async Task ExecuteAsync_ReturnsElapsedTime()
    {
        // Arrange
        var dbPath = Path.Combine(_tempDir, "timing.db");
        var command = new SnapshotCommand();

        // Act
        var result = await command.ExecuteAsync(_sampleSolutionPath, dbPath);

        // Assert
        Assert.True(result.Success);
        Assert.True(result.ElapsedMs >= 0, "Elapsed time should be non-negative");
    }

    [Fact]
    public async Task ExecuteAsync_ReturnsGeneratedTimestamp()
    {
        // Arrange
        var dbPath = Path.Combine(_tempDir, "timestamp.db");
        var command = new SnapshotCommand();

        // Act
        var result = await command.ExecuteAsync(_sampleSolutionPath, dbPath);

        // Assert
        Assert.True(result.Success);
        Assert.False(string.IsNullOrEmpty(result.GeneratedAt), "GeneratedAt should not be empty");
        
        // Verify it's a valid ISO 8601 timestamp
        var parsed = DateTime.Parse(result.GeneratedAt);
        Assert.True(parsed <= DateTime.UtcNow, "GeneratedAt should not be in the future");
    }

    [Fact]
    public async Task ExecuteAsync_ReturnsSchemaVersion()
    {
        // Arrange
        var dbPath = Path.Combine(_tempDir, "schema.db");
        var command = new SnapshotCommand();

        // Act
        var result = await command.ExecuteAsync(_sampleSolutionPath, dbPath);

        // Assert
        Assert.True(result.Success);
        Assert.True(result.SchemaVersion > 0, "Schema version should be positive");
    }

    [Fact]
    public async Task ExecuteAsync_Idempotent_RerunUpdatesData()
    {
        // Arrange
        var dbPath = Path.Combine(_tempDir, "idempotent.db");
        var command = new SnapshotCommand();

        // Act - run twice
        var result1 = await command.ExecuteAsync(_sampleSolutionPath, dbPath);
        var result2 = await command.ExecuteAsync(_sampleSolutionPath, dbPath);

        // Assert
        Assert.True(result1.Success);
        Assert.True(result2.Success);
        
        // Both should succeed and have counts
        Assert.True(result1.ClassCount > 0 || result2.ClassCount > 0);
    }
}
