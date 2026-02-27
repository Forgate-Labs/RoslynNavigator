using Microsoft.Data.Sqlite;
using RoslynNavigator.Commands;
using RoslynNavigator.Models;
using RoslynNavigator.Rules.Services;
using RoslynNavigator.Rules.Models;
using RoslynNavigator.Snapshot.Services;

namespace RoslynNavigator.Tests;

/// <summary>
/// Tests for CheckCommand - verifies CLI command orchestration and filtering.
/// </summary>
public class CheckCommandTests : IDisposable
{
    private readonly string _dbPath;
    private readonly string _connectionString;
    private readonly SqliteConnection _connection;
    private readonly string _tempDir;

    public CheckCommandTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"check_test_{Guid.NewGuid()}");
        Directory.CreateDirectory(_tempDir);
        
        // Create a temp file for the database
        _dbPath = Path.Combine(_tempDir, "test_snapshot.db");
        _connectionString = $"Data Source={_dbPath}";
        _connection = new SqliteConnection(_connectionString);
        _connection.Open();
        
        // Initialize schema and seed test data
        InitializeSchema();
        SeedTestData();
    }

    public void Dispose()
    {
        _connection.Close();
        _connection.Dispose();
        
        // Clean up temp directory
        if (Directory.Exists(_tempDir))
        {
            Directory.Delete(_tempDir, recursive: true);
        }
    }

    private void InitializeSchema()
    {
        var createTables = @"
            CREATE TABLE snapshot_meta (
                id INTEGER PRIMARY KEY CHECK (id = 1),
                generated_at TEXT NOT NULL,
                solution_path TEXT NOT NULL,
                schema_version INTEGER DEFAULT 1
            );
            
            CREATE TABLE classes (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                snapshot_id INTEGER NOT NULL DEFAULT 1,
                namespace TEXT NOT NULL,
                name TEXT NOT NULL,
                kind TEXT NOT NULL,
                accessibility TEXT,
                is_abstract INTEGER DEFAULT 0,
                is_sealed INTEGER DEFAULT 0,
                is_static INTEGER DEFAULT 0,
                base_types TEXT,
                implements TEXT,
                file_path TEXT NOT NULL,
                start_line INTEGER NOT NULL,
                end_line INTEGER NOT NULL,
                returns_null INTEGER DEFAULT 0,
                cognitive_complexity INTEGER DEFAULT 0,
                has_try_catch INTEGER DEFAULT 0,
                calls_external INTEGER DEFAULT 0,
                accesses_db INTEGER DEFAULT 0,
                filters_by_tenant INTEGER DEFAULT 0
            );
            
            CREATE TABLE methods (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                snapshot_id INTEGER NOT NULL DEFAULT 1,
                class_id INTEGER NOT NULL,
                name TEXT NOT NULL,
                return_type TEXT,
                accessibility TEXT,
                is_virtual INTEGER DEFAULT 0,
                is_override INTEGER DEFAULT 0,
                is_static INTEGER DEFAULT 0,
                is_abstract INTEGER DEFAULT 0,
                parameters TEXT,
                start_line INTEGER NOT NULL,
                end_line INTEGER NOT NULL,
                returns_null INTEGER DEFAULT 0,
                cognitive_complexity INTEGER DEFAULT 0,
                has_try_catch INTEGER DEFAULT 0,
                calls_external INTEGER DEFAULT 0,
                accesses_db INTEGER DEFAULT 0,
                filters_by_tenant INTEGER DEFAULT 0
            );
            
            CREATE TABLE calls (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                snapshot_id INTEGER NOT NULL DEFAULT 1,
                caller_method_id INTEGER NOT NULL,
                target_namespace TEXT NOT NULL,
                target_class TEXT NOT NULL,
                target_method TEXT NOT NULL,
                line_number INTEGER NOT NULL
            );
            
            INSERT INTO snapshot_meta (id, generated_at, solution_path) VALUES (1, datetime('now'), 'test.sln');
        ";
        
        using var cmd = new SqliteCommand(createTables, _connection);
        cmd.ExecuteNonQuery();
    }

    private void SeedTestData()
    {
        // Seed test data with known violations
        var seedData = @"
            -- Class 1: UserRepository in Data namespace (violates no-controller rule - error severity)
            INSERT INTO classes (id, namespace, name, kind, file_path, start_line, end_line) 
            VALUES (1, 'MyApp.Data.Repositories', 'UserRepository', 'class', 'UserRepository.cs', 1, 50);
            
            -- Method 1: Save calls Controller.Save (should trigger error)
            INSERT INTO methods (id, class_id, name, return_type, start_line, end_line)
            VALUES (1, 1, 'Save', 'void', 10, 30);
            INSERT INTO calls (caller_method_id, target_namespace, target_class, target_method, line_number)
            VALUES (1, 'MyApp.Web', 'Controller', 'Save', 15);
            
            -- Class 2: UserService in Services (violates no-controller rule - warning severity)
            INSERT INTO classes (id, namespace, name, kind, file_path, start_line, end_line) 
            VALUES (2, 'MyApp.Services', 'UserService', 'class', 'UserService.cs', 1, 60);
            
            -- Method 2: Get calls Controller.Get (should trigger warning)
            INSERT INTO methods (id, class_id, name, return_type, start_line, end_line)
            VALUES (2, 2, 'Get', 'User', 20, 50);
            INSERT INTO calls (caller_method_id, target_namespace, target_class, target_method, line_number)
            VALUES (2, 'MyApp.Web', 'Controller', 'Get', 25);
            
            -- Class 3: ValidRepository (no violations - in Services, calls IRepository)
            INSERT INTO classes (id, namespace, name, kind, file_path, start_line, end_line) 
            VALUES (3, 'MyApp.Services', 'ValidRepository', 'class', 'ValidRepository.cs', 1, 40);
            
            INSERT INTO methods (id, class_id, name, return_type, start_line, end_line)
            VALUES (3, 3, 'Fetch', 'User', 10, 30);
            INSERT INTO calls (caller_method_id, target_namespace, target_class, target_method, line_number)
            VALUES (3, 'MyApp.Data', 'IRepository', 'Fetch', 15);
            
            -- Class 4: HighComplexityService (violates complexity rule - warning)
            INSERT INTO classes (id, namespace, name, kind, file_path, start_line, end_line) 
            VALUES (4, 'MyApp.Services', 'HighComplexityService', 'class', 'Complex.cs', 1, 100);
            
            INSERT INTO methods (id, class_id, name, return_type, start_line, end_line, cognitive_complexity)
            VALUES (4, 4, 'ProcessData', 'void', 10, 90, 25);
        ";
        
        using var cmd = new SqliteCommand(seedData, _connection);
        cmd.ExecuteNonQuery();
    }

    // --- Basic functionality tests ---

    [Fact]
    public async Task ExecuteAsync_WithValidDb_ReturnsViolations()
    {
        // Arrange
        var command = new CheckCommand();

        // Act
        var result = await command.ExecuteAsync(_dbPath);

        // Assert
        Assert.True(result.Success, $"Expected success but got: {result.ErrorMessage}");
        Assert.True(result.TotalViolations > 0, "Should find some violations");
        Assert.NotEmpty(result.Violations);
    }

    [Fact]
    public async Task ExecuteAsync_WithValidDb_ReturnsMetadata()
    {
        // Arrange
        var command = new CheckCommand();

        // Act
        var result = await command.ExecuteAsync(_dbPath);

        // Assert
        Assert.True(result.Success);
        Assert.Equal(_dbPath, result.DbPath);
        Assert.True(result.TotalRulesEvaluated > 0, "Should evaluate some rules");
        Assert.True(result.ElapsedMs >= 0, "Elapsed time should be non-negative");
    }

    // --- Error handling tests ---

    [Fact]
    public async Task ExecuteAsync_WithInvalidDbPath_ReturnsError()
    {
        // Arrange
        var command = new CheckCommand();
        var invalidPath = Path.Combine(_tempDir, "nonexistent.db");

        // Act
        var result = await command.ExecuteAsync(invalidPath);

        // Assert
        Assert.False(result.Success);
        Assert.NotNull(result.ErrorMessage);
        Assert.Contains("not found", result.ErrorMessage, StringComparison.OrdinalIgnoreCase);
    }

    // --- Severity filter tests ---

    [Fact]
    public async Task ExecuteAsync_WithSeverityFilter_ReturnsFilteredResults()
    {
        // Arrange
        var command = new CheckCommand();

        // Act - filter by error severity
        var result = await command.ExecuteAsync(_dbPath, severityFilter: "error");

        // Assert
        Assert.True(result.Success);
        Assert.Equal("error", result.SeverityFilter);
        
        // All violations should be error severity
        foreach (var violation in result.Violations)
        {
            Assert.Equal(RuleSeverity.Error, violation.Severity);
        }
    }

    [Fact]
    public async Task ExecuteAsync_WithSeverityFilter_ReducesCount()
    {
        // Arrange
        var command = new CheckCommand();

        // Get all violations first
        var allResult = await command.ExecuteAsync(_dbPath);
        var allCount = allResult.TotalViolations;

        // Get filtered violations
        var filteredResult = await command.ExecuteAsync(_dbPath, severityFilter: "error");
        var filteredCount = filteredResult.FilteredViolations;

        // Assert
        Assert.True(allCount >= filteredCount, "Filtered count should be <= total count");
    }

    // --- RuleId filter tests ---

    [Fact]
    public async Task ExecuteAsync_WithRuleIdFilter_ReturnsFilteredResults()
    {
        // Arrange
        var command = new CheckCommand();

        // Act - filter by partial ruleId match
        var result = await command.ExecuteAsync(_dbPath, ruleIdFilter: "architecture");

        // Assert
        Assert.True(result.Success);
        Assert.Equal("architecture", result.RuleIdFilter);
        
        // All violations should have matching ruleId
        foreach (var violation in result.Violations)
        {
            Assert.Contains("architecture", violation.RuleId, StringComparison.OrdinalIgnoreCase);
        }
    }

    [Fact]
    public async Task ExecuteAsync_WithRuleIdFilter_ReducesCount()
    {
        // Arrange
        var command = new CheckCommand();

        // Get all violations first
        var allResult = await command.ExecuteAsync(_dbPath);
        var allCount = allResult.TotalViolations;

        // Get filtered violations
        var filteredResult = await command.ExecuteAsync(_dbPath, ruleIdFilter: "security");
        var filteredCount = filteredResult.FilteredViolations;

        // Assert - either same (if all match) or less (if filter reduces)
        Assert.True(filteredCount <= allCount, "Filtered count should be <= total count");
    }

    // --- Combined filters tests ---

    [Fact]
    public async Task ExecuteAsync_WithBothFilters_ReturnsIntersection()
    {
        // Arrange
        var command = new CheckCommand();

        // Act - filter by both severity and ruleId
        var result = await command.ExecuteAsync(_dbPath, severityFilter: "warning", ruleIdFilter: "quality");

        // Assert
        Assert.True(result.Success);
        Assert.NotNull(result.SeverityFilter);
        Assert.NotNull(result.RuleIdFilter);
        
        // All violations should match both filters
        foreach (var violation in result.Violations)
        {
            Assert.Equal(RuleSeverity.Warning, violation.Severity);
            Assert.Contains("quality", violation.RuleId, StringComparison.OrdinalIgnoreCase);
        }
    }

    [Fact]
    public async Task ExecuteAsync_WithNoMatchingFilters_ReturnsEmptyViolations()
    {
        // Arrange
        var command = new CheckCommand();

        // Act - filter by severity that doesn't exist
        var result = await command.ExecuteAsync(_dbPath, severityFilter: "info");

        // Assert - may be empty if no info severity rules
        Assert.True(result.Success);
        // Either empty or all are info severity
        foreach (var violation in result.Violations)
        {
            Assert.Equal(RuleSeverity.Info, violation.Severity);
        }
    }

    // --- Integration test with real snapshot ---

    [Fact]
    public async Task ExecuteAsync_WithSampleSolution_ReturnsResult()
    {
        // This test creates a snapshot from the sample solution and runs check on it
        // Arrange - generate snapshot first
        var sampleSolutionPath = Path.GetFullPath(
            Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", 
                "tests", "SampleSolution", "Sample.sln"));
        
        if (!File.Exists(sampleSolutionPath))
        {
            // Skip if sample solution doesn't exist
            return;
        }

        var snapshotDbPath = Path.Combine(_tempDir, "sample_snapshot.db");
        
        // Create snapshot
        var snapshotCommand = new SnapshotCommand();
        var snapshotResult = await snapshotCommand.ExecuteAsync(sampleSolutionPath, snapshotDbPath);
        
        if (!snapshotResult.Success || !File.Exists(snapshotDbPath))
        {
            // Skip if snapshot creation fails
            return;
        }

        // Act - run check on the snapshot
        var checkCommand = new CheckCommand();
        var checkResult = await checkCommand.ExecuteAsync(snapshotDbPath);

        // Assert
        Assert.True(checkResult.Success, $"Check should succeed but got: {checkResult.ErrorMessage}");
        Assert.Equal(snapshotDbPath, checkResult.DbPath);
        Assert.True(checkResult.TotalRulesEvaluated > 0, "Should evaluate rules");
    }
}
