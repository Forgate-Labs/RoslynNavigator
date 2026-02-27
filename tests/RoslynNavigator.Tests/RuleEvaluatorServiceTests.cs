using Microsoft.Data.Sqlite;
using RoslynNavigator.Models;
using RoslynNavigator.Services;

namespace RoslynNavigator.Tests;

/// <summary>
/// Tests for RuleEvaluatorService - verifies evaluation against snapshot DB.
/// </summary>
public class RuleEvaluatorServiceTests : IDisposable
{
    private readonly string _connectionString;
    private readonly SqliteConnection _connection;

    public RuleEvaluatorServiceTests()
    {
        // Create in-memory database for testing
        _connectionString = "Data Source=:memory:";
        _connection = new SqliteConnection(_connectionString);
        _connection.Open();
        
        // Initialize schema
        InitializeSchema();
    }

    public void Dispose()
    {
        _connection.Close();
        _connection.Dispose();
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
        // Seed test data for violation detection
        var seedData = @"
            -- Add a class that calls Controller.*
            INSERT INTO classes (id, namespace, name, kind, file_path, start_line, end_line) 
            VALUES (1, 'MyApp.Data.Repositories', 'UserRepository', 'class', 'UserRepository.cs', 1, 50);
            
            -- Add a method that calls Controller.Save
            INSERT INTO methods (id, class_id, name, return_type, start_line, end_line)
            VALUES (1, 1, 'Save', 'void', 10, 30);
            
            -- Add a call from UserRepository.Save to Controller.Save
            INSERT INTO calls (caller_method_id, target_namespace, target_class, target_method, line_number)
            VALUES (1, 'MyApp.Web', 'Controller', 'Save', 15);
            
            -- Add another class that matches the pattern
            INSERT INTO classes (id, namespace, name, kind, file_path, start_line, end_line) 
            VALUES (2, 'MyApp.Services', 'OrderService', 'class', 'OrderService.cs', 1, 60);
            
            INSERT INTO methods (id, class_id, name, return_type, start_line, end_line)
            VALUES (2, 2, 'Process', 'void', 20, 50);
            
            -- Call from OrderService to IController (should be excluded by NOT)
            INSERT INTO calls (caller_method_id, target_namespace, target_class, target_method, line_number)
            VALUES (2, 'MyApp.Web', 'IController', 'Process', 25);
        ";
        
        using var cmd = new SqliteCommand(seedData, _connection);
        cmd.ExecuteNonQuery();
    }

    // --- Integration tests ---

    [Fact]
    public void Evaluate_WildcardCallsPattern_FindsViolations()
    {
        // Arrange
        SeedTestData();
        var evaluator = new RuleEvaluatorService(_connectionString);
        var rule = new RuleDefinition
        {
            Id = "test-001",
            Title = "Test rule",
            Severity = "error",
            Message = "Repository should not call Controller",
            Predicate = new RulePredicate { Calls = "Controller.*" }
        };

        // Act
        var result = evaluator.Evaluate(rule);

        // Assert
        Assert.True(result.Success);
        Assert.NotEmpty(result.Violations);
        Assert.Contains(result.Violations, v => v.ClassName == "UserRepository");
    }

    [Fact]
    public void Evaluate_NotPredicate_ExcludesNestedMatch()
    {
        // Arrange
        SeedTestData();
        var evaluator = new RuleEvaluatorService(_connectionString);
        
        // Rule: calls Controller.* but NOT IController
        var rule = new RuleDefinition
        {
            Id = "test-002",
            Title = "Test rule with NOT",
            Severity = "error",
            Message = "Repository should not call Controller except IController",
            Predicate = new RulePredicate
            {
                Calls = "Controller.*",
                Not = new RulePredicate { Calls = "IController" }
            }
        };

        // Act
        var result = evaluator.Evaluate(rule);

        // Assert - should NOT find OrderService because it calls IController (excluded by NOT)
        Assert.True(result.Success);
        // UserRepository calls Controller.Save (not IController) - should be violation
        // OrderService calls IController - should NOT be violation
        Assert.Contains(result.Violations, v => v.ClassName == "UserRepository");
        Assert.DoesNotContain(result.Violations, v => v.ClassName == "OrderService");
    }

    [Fact]
    public void Evaluate_NoPredicateMatch_ReturnsEmptyViolations()
    {
        // Arrange
        SeedTestData();
        var evaluator = new RuleEvaluatorService(_connectionString);
        var rule = new RuleDefinition
        {
            Id = "test-003",
            Title = "No match rule",
            Severity = "warning",
            Message = "This should not match anything",
            Predicate = new RulePredicate { Calls = "NonExistent.*" }
        };

        // Act
        var result = evaluator.Evaluate(rule);

        // Assert
        Assert.True(result.Success);
        Assert.Empty(result.Violations);
    }

    // --- Returns null tests ---

    [Fact]
    public void Evaluate_ReturnsNullPredicate_FindsViolations()
    {
        // Arrange - add a class with returns_null = 1
        var seedData = @"
            INSERT INTO classes (namespace, name, kind, file_path, start_line, end_line, returns_null)
            VALUES ('MyApp.Services', 'UserService', 'class', 'UserService.cs', 1, 50, 1);
        ";
        using var cmd = new SqliteCommand(seedData, _connection);
        cmd.ExecuteNonQuery();

        var evaluator = new RuleEvaluatorService(_connectionString);
        var rule = new RuleDefinition
        {
            Id = "test-004",
            Title = "Returns null test",
            Severity = "warning",
            Message = "Method should not return null",
            Predicate = new RulePredicate { ReturnsNull = true }
        };

        // Act
        var result = evaluator.Evaluate(rule);

        // Assert
        Assert.True(result.Success);
        Assert.NotEmpty(result.Violations);
    }

    // --- Cognitive complexity tests ---

    [Fact]
    public void Evaluate_CognitiveComplexity_FindsViolations()
    {
        // Arrange - add a class with high complexity
        var seedData = @"
            INSERT INTO classes (namespace, name, kind, file_path, start_line, end_line, cognitive_complexity)
            VALUES ('MyApp.Services', 'ComplexService', 'class', 'ComplexService.cs', 1, 100, 25);
        ";
        using var cmd = new SqliteCommand(seedData, _connection);
        cmd.ExecuteNonQuery();

        var evaluator = new RuleEvaluatorService(_connectionString);
        var rule = new RuleDefinition
        {
            Id = "test-005",
            Title = "Complexity test",
            Severity = "warning",
            Message = "Method too complex",
            Predicate = new RulePredicate { CognitiveComplexity = 10 }
        };

        // Act
        var result = evaluator.Evaluate(rule);

        // Assert
        Assert.True(result.Success);
        Assert.NotEmpty(result.Violations);
    }

    // --- FromNamespace tests ---

    [Fact]
    public void Evaluate_FromNamespaceFilter_FiltersCorrectly()
    {
        // Arrange - add classes in different namespaces
        var seedData = @"
            INSERT INTO classes (namespace, name, kind, file_path, start_line, end_line) 
            VALUES ('MyApp.Data.Repositories', 'UserRepo', 'class', 'UserRepo.cs', 1, 50);
            INSERT INTO classes (namespace, name, kind, file_path, start_line, end_line) 
            VALUES ('MyApp.Services', 'UserService', 'class', 'UserService.cs', 1, 50);
        ";
        using var cmd = new SqliteCommand(seedData, _connection);
        cmd.ExecuteNonQuery();

        var evaluator = new RuleEvaluatorService(_connectionString);
        var rule = new RuleDefinition
        {
            Id = "test-006",
            Title = "Namespace filter test",
            Severity = "warning",
            Message = "Data namespace",
            Predicate = new RulePredicate { FromNamespace = "*.Data.*" }
        };

        // Act
        var result = evaluator.Evaluate(rule);

        // Assert
        Assert.True(result.Success);
        Assert.Single(result.Violations);
        Assert.Equal("UserRepo", result.Violations[0].ClassName);
    }

    // --- Read-only enforcement ---

    [Fact]
    public void Evaluate_QueryIsReadOnly_DoesNotModifyDatabase()
    {
        // Arrange
        var evaluator = new RuleEvaluatorService(_connectionString);
        var rule = new RuleDefinition
        {
            Id = "test-007",
            Title = "Readonly test",
            Severity = "info",
            Message = "Test",
            Predicate = new RulePredicate { Calls = "Test.*" }
        };

        // Get initial row count
        using var countCmd = new SqliteCommand("SELECT COUNT(*) FROM classes", _connection);
        var initialCount = Convert.ToInt32(countCmd.ExecuteScalar());

        // Act
        var result = evaluator.Evaluate(rule);

        // Assert - count should be unchanged
        using var verifyCmd = new SqliteCommand("SELECT COUNT(*) FROM classes", _connection);
        var finalCount = Convert.ToInt32(verifyCmd.ExecuteScalar());
        
        Assert.Equal(initialCount, finalCount);
    }
}
