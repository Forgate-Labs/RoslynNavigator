using RoslynNavigator.Snapshot.Services;

namespace RoslynNavigator.Tests;

public class SnapshotSchemaServiceTests : IDisposable
{
    private readonly string _tempDir;
    private readonly SnapshotSchemaService _schemaService;
    private readonly SnapshotPathService _pathService;

    public SnapshotSchemaServiceTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(_tempDir);
        _pathService = new SnapshotPathService();
        _schemaService = new SnapshotSchemaService(_pathService);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
            Directory.Delete(_tempDir, recursive: true);
    }

    // --- InitializeDatabase tests ---

    [Fact]
    public void InitializeDatabase_CreatesDatabaseFile()
    {
        var dbPath = Path.Combine(_tempDir, "test.db");
        var solutionPath = Path.Combine(_tempDir, "solution.sln");

        _schemaService.InitializeDatabase(dbPath, solutionPath);

        Assert.True(File.Exists(dbPath));
    }

    [Fact]
    public void InitializeDatabase_CreatesAllRequiredTables()
    {
        var dbPath = Path.Combine(_tempDir, "test.db");
        var solutionPath = Path.Combine(_tempDir, "solution.sln");

        _schemaService.InitializeDatabase(dbPath, solutionPath);

        Assert.True(_schemaService.ValidateTablesExist(dbPath));
    }

    [Fact]
    public void InitializeDatabase_SecondRunIsIdempotent()
    {
        var dbPath = Path.Combine(_tempDir, "test.db");
        var solutionPath = Path.Combine(_tempDir, "solution.sln");

        // First initialization
        _schemaService.InitializeDatabase(dbPath, solutionPath);
        var firstMeta = _schemaService.GetSnapshotMeta(dbPath);
        
        // Allow some time to pass to ensure different timestamp
        Thread.Sleep(10);

        // Second initialization should not fail
        _schemaService.InitializeDatabase(dbPath, solutionPath);
        var secondMeta = _schemaService.GetSnapshotMeta(dbPath);

        // Both should succeed and have valid metadata
        Assert.NotNull(firstMeta);
        Assert.NotNull(secondMeta);
        Assert.True(_schemaService.ValidateTablesExist(dbPath));
    }

    [Fact]
    public void InitializeDatabase_WritesSnapshotMeta()
    {
        var dbPath = Path.Combine(_tempDir, "test.db");
        var solutionPath = Path.Combine(_tempDir, "MySolution.sln");

        _schemaService.InitializeDatabase(dbPath, solutionPath);

        var meta = _schemaService.GetSnapshotMeta(dbPath);
        
        Assert.NotNull(meta);
        Assert.Equal(solutionPath, meta.SolutionPath);
        Assert.False(string.IsNullOrEmpty(meta.GeneratedAt));
        Assert.Equal(1, meta.SchemaVersion);
    }

    [Fact]
    public void InitializeDatabase_UpdatesSnapshotMetaOnReinit()
    {
        var dbPath = Path.Combine(_tempDir, "test.db");
        var solutionPath1 = Path.Combine(_tempDir, "solution1.sln");
        var solutionPath2 = Path.Combine(_tempDir, "solution2.sln");

        _schemaService.InitializeDatabase(dbPath, solutionPath1);
        var firstMeta = _schemaService.GetSnapshotMeta(dbPath);
        
        Thread.Sleep(10);
        
        _schemaService.InitializeDatabase(dbPath, solutionPath2);
        var secondMeta = _schemaService.GetSnapshotMeta(dbPath);

        Assert.NotNull(firstMeta);
        Assert.NotNull(secondMeta);
        Assert.Equal(solutionPath2, secondMeta.SolutionPath);
        Assert.True(string.Compare(firstMeta.GeneratedAt, secondMeta.GeneratedAt, StringComparison.Ordinal) < 0,
            "Second timestamp should be newer");
    }

    // --- InitializeDefaultDatabase tests ---

    [Fact]
    public void InitializeDefaultDatabase_CreatesDbInExpectedLocation()
    {
        var solutionPath = Path.Combine(_tempDir, "MySolution.sln");
        var expectedPath = Path.Combine(_tempDir, ".roslyn-nav", "snapshots", "MySolution.snapshot.db");

        var resultPath = _schemaService.InitializeDefaultDatabase(solutionPath);

        Assert.Equal(expectedPath, resultPath);
        Assert.True(File.Exists(resultPath));
    }

    [Fact]
    public void InitializeDefaultDatabase_CreatesParentDirectories()
    {
        var solutionPath = Path.Combine(_tempDir, "MySolution.sln");

        _schemaService.InitializeDefaultDatabase(solutionPath);

        var expectedDir = Path.Combine(_tempDir, ".roslyn-nav", "snapshots");
        Assert.True(Directory.Exists(expectedDir));
    }

    // --- ValidateTablesExist tests ---

    [Fact]
    public void ValidateTablesExist_ReturnsTrueForValidDatabase()
    {
        var dbPath = Path.Combine(_tempDir, "test.db");
        var solutionPath = "/test/solution.sln";

        _schemaService.InitializeDatabase(dbPath, solutionPath);

        Assert.True(_schemaService.ValidateTablesExist(dbPath));
    }

    [Fact]
    public void ValidateTablesExist_ReturnsFalseForEmptyDatabase()
    {
        var dbPath = Path.Combine(_tempDir, "empty.db");
        
        // Create an empty database file
        File.WriteAllBytes(dbPath, Array.Empty<byte>());

        Assert.False(_schemaService.ValidateTablesExist(dbPath));
    }

    // --- GetSnapshotMeta tests ---

    [Fact]
    public void GetSnapshotMeta_ReturnsCorrectData()
    {
        var dbPath = Path.Combine(_tempDir, "test.db");
        var solutionPath = Path.Combine(_tempDir, "AnotherSolution.sln");

        _schemaService.InitializeDatabase(dbPath, solutionPath);

        var meta = _schemaService.GetSnapshotMeta(dbPath);

        Assert.NotNull(meta);
        Assert.Equal(1, meta.Id);
        Assert.Equal(solutionPath, meta.SolutionPath);
    }

    [Fact]
    public void GetSnapshotMeta_ReturnsNullForNonExistentDb()
    {
        var dbPath = Path.Combine(_tempDir, "nonexistent.db");

        // Don't create the file - it shouldn't exist
        Assert.False(File.Exists(dbPath));

        var meta = _schemaService.GetSnapshotMeta(dbPath);

        Assert.Null(meta);
    }

    // --- SnapshotPathService tests ---

    [Fact]
    public void ResolveDefaultPath_ReturnsCorrectPath()
    {
        var solutionPath = Path.Combine(_tempDir, "MyApp.sln");
        
        var result = _pathService.ResolveDefaultPath(solutionPath);

        var expectedPath = Path.Combine(_tempDir, ".roslyn-nav", "snapshots", "MyApp.snapshot.db");
        Assert.Equal(expectedPath, result);
    }

    [Fact]
    public void ResolveDefaultPath_ThrowsForEmptySolutionPath()
    {
        Assert.Throws<ArgumentException>(() => _pathService.ResolveDefaultPath(""));
    }

    [Fact]
    public void ResolveDefaultPath_ThrowsForNullSolutionPath()
    {
        Assert.Throws<ArgumentException>(() => _pathService.ResolveDefaultPath(null!));
    }

    [Fact]
    public void EnsureParentDirectoryExists_CreatesDirectory()
    {
        var dbPath = Path.Combine(_tempDir, "subdir", "nested", "test.db");

        _pathService.EnsureParentDirectoryExists(dbPath);

        Assert.True(Directory.Exists(Path.GetDirectoryName(dbPath)));
    }

    [Fact]
    public void NormalizePath_ConvertsRelativeToAbsolute()
    {
        var relativePath = "test/file.db";
        
        var result = _pathService.NormalizePath(relativePath);

        Assert.True(Path.IsPathRooted(result));
    }

    [Fact]
    public void IsDefaultPath_ReturnsTrueForMatchingPaths()
    {
        var solutionPath = Path.Combine(_tempDir, "Solution.sln");
        var dbPath = Path.Combine(_tempDir, ".roslyn-nav", "snapshots", "Solution.snapshot.db");

        var result = _pathService.IsDefaultPath(dbPath, solutionPath);

        Assert.True(result);
    }

    [Fact]
    public void IsDefaultPath_ReturnsFalseForDifferentPaths()
    {
        var solutionPath = Path.Combine(_tempDir, "Solution.sln");
        var dbPath = Path.Combine(_tempDir, "other", "location", "db.db");

        var result = _pathService.IsDefaultPath(dbPath, solutionPath);

        Assert.False(result);
    }
}
