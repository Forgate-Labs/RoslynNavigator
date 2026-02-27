using Microsoft.Data.Sqlite;
using RoslynNavigator.Commands;
using RoslynNavigator.Services;

namespace RoslynNavigator.Tests;

/// <summary>
/// Tests for SnapshotQueryCommand - verifies query execution and read-only enforcement.
/// </summary>
public class SnapshotQueryCommandTests : IDisposable
{
    private readonly string _dbPath;
    private readonly string _connectionString;
    private readonly SqliteConnection _connection;

    public SnapshotQueryCommandTests()
    {
        // Create a temp file for the database
        _dbPath = Path.Combine(Path.GetTempPath(), $"test_{Guid.NewGuid()}.db");
        _connectionString = $"Data Source={_dbPath}";
        _connection = new SqliteConnection(_connectionString);
        _connection.Open();
        
        // Initialize schema
        InitializeSchema();
    }

    public void Dispose()
    {
        _connection.Close();
        _connection.Dispose();
        
        // Clean up temp file
        if (File.Exists(_dbPath))
        {
            File.Delete(_dbPath);
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
                end_line INTEGER NOT NULL
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
                end_line INTEGER NOT NULL
            );
            
            INSERT INTO snapshot_meta (id, generated_at, solution_path) VALUES (1, datetime('now'), 'test.sln');
            
            -- Seed test data
            INSERT INTO classes (namespace, name, kind, file_path, start_line, end_line) 
            VALUES ('MyApp.Services', 'UserService', 'class', 'UserService.cs', 1, 50);
            INSERT INTO classes (namespace, name, kind, file_path, start_line, end_line) 
            VALUES ('MyApp.Data', 'UserRepository', 'class', 'UserRepository.cs', 1, 30);
            INSERT INTO classes (namespace, name, kind, file_path, start_line, end_line) 
            VALUES ('MyApp.Controllers', 'HomeController', 'class', 'HomeController.cs', 1, 100);
            
            INSERT INTO methods (class_id, name, return_type, start_line, end_line)
            VALUES (1, 'GetUser', 'User?', 10, 20);
            INSERT INTO methods (class_id, name, return_type, start_line, end_line)
            VALUES (1, 'SaveUser', 'void', 25, 35);
            INSERT INTO methods (class_id, name, return_type, start_line, end_line)
            VALUES (2, 'FindById', 'User?', 5, 15);
            INSERT INTO methods (class_id, name, return_type, start_line, end_line)
            VALUES (3, 'Index', 'IActionResult', 10, 25);
        ";
        
        using var cmd = new SqliteCommand(createTables, _connection);
        cmd.ExecuteNonQuery();
    }

    // --- Successful query tests ---

    [Fact]
    public async Task Execute_SelectAllClasses_ReturnsJsonArray()
    {
        // Arrange
        var command = new SnapshotQueryCommand();

        // Act
        var result = await command.ExecuteAsync("SELECT * FROM classes", _dbPath);

        // Assert
        Assert.True(result.Success, result.ErrorMessage);
        Assert.Equal(3, result.RowCount);
        Assert.Equal(3, result.Rows.Count);
        Assert.NotNull(result.Rows[0]["name"]);
    }

    [Fact]
    public async Task Execute_SelectWithProjection_ReturnsCorrectColumns()
    {
        // Arrange
        var command = new SnapshotQueryCommand();

        // Act
        var result = await command.ExecuteAsync("SELECT namespace, name FROM classes ORDER BY name", _dbPath);

        // Assert
        Assert.True(result.Success, result.ErrorMessage);
        Assert.Equal(3, result.RowCount);
        // Check column names are preserved
        Assert.True(result.Rows[0].ContainsKey("namespace"));
        Assert.True(result.Rows[0].ContainsKey("name"));
        Assert.False(result.Rows[0].ContainsKey("kind"));
    }

    [Fact]
    public async Task Execute_SelectWithJoin_ReturnsCorrectData()
    {
        // Arrange
        var command = new SnapshotQueryCommand();

        // Act
        var result = await command.ExecuteAsync(
            "SELECT c.name as class_name, m.name as method_name FROM classes c JOIN methods m ON c.id = m.class_id WHERE m.name = 'GetUser'",
            _dbPath);

        // Assert
        Assert.True(result.Success, result.ErrorMessage);
        Assert.Equal(1, result.RowCount);
        Assert.Equal("UserService", result.Rows[0]["class_name"]);
        Assert.Equal("GetUser", result.Rows[0]["method_name"]);
    }

    [Fact]
    public async Task Execute_EmptyResultSet_ReturnsZeroRows()
    {
        // Arrange
        var command = new SnapshotQueryCommand();

        // Act
        var result = await command.ExecuteAsync("SELECT * FROM classes WHERE name = 'NonExistent'", _dbPath);

        // Assert
        Assert.True(result.Success, result.ErrorMessage);
        Assert.Equal(0, result.RowCount);
        Assert.Empty(result.Rows);
    }

    [Fact]
    public async Task Execute_SelectWithAggregation_ReturnsCorrectResult()
    {
        // Arrange
        var command = new SnapshotQueryCommand();

        // Act
        var result = await command.ExecuteAsync("SELECT COUNT(*) as cnt FROM classes", _dbPath);

        // Assert
        Assert.True(result.Success, result.ErrorMessage);
        Assert.Equal(1, result.RowCount);
        Assert.Equal(3L, result.Rows[0]["cnt"]);
    }

    // --- Read-only enforcement tests ---

    [Fact]
    public async Task Execute_InsertQuery_IsRejected()
    {
        // Arrange
        var command = new SnapshotQueryCommand();

        // Act
        var result = await command.ExecuteAsync("INSERT INTO classes (namespace, name, kind, file_path, start_line, end_line) VALUES ('Test', 'TestClass', 'class', 'test.cs', 1, 10)", _dbPath);

        // Assert
        Assert.False(result.Success);
        Assert.Contains("INSERT", result.ErrorMessage);
    }

    [Fact]
    public async Task Execute_UpdateQuery_IsRejected()
    {
        // Arrange
        var command = new SnapshotQueryCommand();

        // Act
        var result = await command.ExecuteAsync("UPDATE classes SET name = 'Modified' WHERE id = 1", _dbPath);

        // Assert
        Assert.False(result.Success);
        Assert.Contains("UPDATE", result.ErrorMessage);
    }

    [Fact]
    public async Task Execute_DeleteQuery_IsRejected()
    {
        // Arrange
        var command = new SnapshotQueryCommand();

        // Act
        var result = await command.ExecuteAsync("DELETE FROM classes", _dbPath);

        // Assert
        Assert.False(result.Success);
        Assert.Contains("DELETE", result.ErrorMessage);
    }

    [Fact]
    public async Task Execute_PragmaQuery_IsRejected()
    {
        // Arrange
        var command = new SnapshotQueryCommand();

        // Act
        var result = await command.ExecuteAsync("PRAGMA journal_mode=WAL", _dbPath);

        // Assert
        Assert.False(result.Success);
        Assert.Contains("PRAGMA", result.ErrorMessage);
    }

    [Fact]
    public async Task Execute_MultiStatementQuery_IsRejected()
    {
        // Arrange
        var command = new SnapshotQueryCommand();

        // Act
        var result = await command.ExecuteAsync("SELECT 1; SELECT 2", _dbPath);

        // Assert
        Assert.False(result.Success);
        Assert.Contains("Multiple statements", result.ErrorMessage);
    }

    // --- Error handling tests ---

    [Fact]
    public async Task Execute_MissingDbFile_ReturnsError()
    {
        // Arrange
        var command = new SnapshotQueryCommand();
        var nonExistentDb = Path.Combine(Path.GetTempPath(), $"nonexistent_{Guid.NewGuid()}.db");

        // Act
        var result = await command.ExecuteAsync("SELECT * FROM classes", nonExistentDb);

        // Assert
        Assert.False(result.Success);
        Assert.Contains("not found", result.ErrorMessage, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Execute_InvalidSql_ReturnsError()
    {
        // Arrange
        var command = new SnapshotQueryCommand();

        // Act
        var result = await command.ExecuteAsync("SELECT * FROM nonexistent_table", _dbPath);

        // Assert
        Assert.False(result.Success);
        Assert.Contains("failed", result.ErrorMessage, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Execute_EmptySql_ReturnsError()
    {
        // Arrange
        var command = new SnapshotQueryCommand();

        // Act
        var result = await command.ExecuteAsync("", _dbPath);

        // Assert
        Assert.False(result.Success);
        Assert.Contains("empty", result.ErrorMessage, StringComparison.OrdinalIgnoreCase);
    }

    // --- Default path resolution tests ---

    [Fact]
    public async Task Execute_WithSolution_ResolvesDefaultPath()
    {
        // Arrange
        var command = new SnapshotQueryCommand();
        var solutionPath = "test.sln";
        
        // Note: This will fail because the default path won't exist, but it tests the resolution logic
        // Act
        var result = await command.ExecuteAsync("SELECT * FROM classes", null, solutionPath);

        // Assert
        // Should either succeed with resolved path or fail with "not found" for the resolved path
        Assert.NotNull(result.DbPath);
        Assert.Contains("test.snapshot.db", result.DbPath);
    }

    // --- Null handling tests ---

    [Fact]
    public async Task Execute_WithNullValues_HandlesCorrectly()
    {
        // Arrange - add a row with null accessibility
        using var insertCmd = new SqliteCommand(
            "INSERT INTO methods (class_id, name, return_type, accessibility, start_line, end_line) VALUES (1, 'TestMethod', NULL, NULL, 1, 5)",
            _connection);
        insertCmd.ExecuteNonQuery();

        var command = new SnapshotQueryCommand();

        // Act
        var result = await command.ExecuteAsync("SELECT name, return_type, accessibility FROM methods WHERE name = 'TestMethod'", _dbPath);

        // Assert
        Assert.True(result.Success, result.ErrorMessage);
        Assert.Equal(1, result.RowCount);
        Assert.Null(result.Rows[0]["return_type"]);
        Assert.Null(result.Rows[0]["accessibility"]);
    }

    // --- Database immutability tests ---

    [Fact]
    public async Task Execute_MutatingQuery_DoesNotModifyDatabase()
    {
        // Arrange
        var command = new SnapshotQueryCommand();
        
        // Get initial row count
        using var countCmd = new SqliteCommand("SELECT COUNT(*) FROM classes", _connection);
        var initialCount = Convert.ToInt32(countCmd.ExecuteScalar());

        // Act - try to execute mutating SQL
        await command.ExecuteAsync("INSERT INTO classes (namespace, name, kind, file_path, start_line, end_line) VALUES ('X', 'X', 'X', 'x', 1, 1)", _dbPath);

        // Assert - count should be unchanged
        using var verifyCmd = new SqliteCommand("SELECT COUNT(*) FROM classes", _connection);
        var finalCount = Convert.ToInt32(verifyCmd.ExecuteScalar());
        
        Assert.Equal(initialCount, finalCount);
    }

    [Fact]
    public async Task Execute_MultipleSelects_DatabaseUnchanged()
    {
        // Arrange
        var command = new SnapshotQueryCommand();
        
        // Get initial row count
        using var countCmd = new SqliteCommand("SELECT COUNT(*) FROM classes", _connection);
        var initialCount = Convert.ToInt32(countCmd.ExecuteScalar());

        // Act - execute multiple queries
        await command.ExecuteAsync("SELECT * FROM classes", _dbPath);
        await command.ExecuteAsync("SELECT COUNT(*) FROM methods", _dbPath);
        await command.ExecuteAsync("SELECT * FROM classes WHERE id = 1", _dbPath);

        // Assert - count should be unchanged
        using var verifyCmd = new SqliteCommand("SELECT COUNT(*) FROM classes", _connection);
        var finalCount = Convert.ToInt32(verifyCmd.ExecuteScalar());
        
        Assert.Equal(initialCount, finalCount);
    }
}
