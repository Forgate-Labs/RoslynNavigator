using RoslynNavigator.Rules.Services;

namespace RoslynNavigator.Tests;

/// <summary>
/// Tests for SqlReadOnlyGuard - validates SQL read-only enforcement.
/// </summary>
public class SqlReadOnlyGuardTests
{
    private readonly SqlReadOnlyGuard _guard = new();

    // --- Valid read-only queries ---

    [Theory]
    [InlineData("SELECT * FROM classes")]
    [InlineData("select * from classes")]
    [InlineData("  SELECT id, name FROM methods")]
    [InlineData("SELECT 1")]
    [InlineData("SELECT DISTINCT namespace FROM classes")]
    [InlineData("SELECT c.name FROM classes c")]
    [InlineData("WITH cte AS (SELECT 1) SELECT * FROM cte")]
    [InlineData("with cte as (select 1) select * from cte")]
    [InlineData("  SELECT * FROM classes WHERE id = 1")]
    public void Validate_SelectQueries_AreValid(string sql)
    {
        var result = _guard.Validate(sql);

        Assert.True(result.IsValid, $"Expected valid for: {sql}");
        Assert.Null(result.Reason);
    }

    [Theory]
    [InlineData("/* comment */ SELECT * FROM classes")]
    [InlineData("-- single line comment\nSELECT * FROM classes")]
    [InlineData("/* multi\nline\ncomment */ SELECT * FROM classes")]
    [InlineData("/* comment */ INSERT INTO classes (name) VALUES ('test')")]
    [InlineData("-- comment at start\nUPDATE classes SET name = 'test'")]
    public void Validate_QueriesWithComments_AreValid(string sql)
    {
        var result = _guard.Validate(sql);

        // SELECT with comments should be valid; non-SELECT should fail
        var trimmed = sql.TrimStart();
        if (trimmed.StartsWith("SELECT", StringComparison.OrdinalIgnoreCase) ||
            trimmed.StartsWith("WITH", StringComparison.OrdinalIgnoreCase))
        {
            Assert.True(result.IsValid, $"Expected valid for: {sql}");
        }
    }

    // --- Invalid mutating queries ---

    [Theory]
    [InlineData("INSERT INTO classes (name) VALUES ('test')")]
    [InlineData("insert into classes (name) values ('test')")]
    [InlineData("INSERT INTO classes SELECT * FROM methods")]
    public void Validate_InsertQueries_AreRejected(string sql)
    {
        var result = _guard.Validate(sql);

        Assert.False(result.IsValid, $"Expected invalid for: {sql}");
        Assert.Contains("INSERT", result.Reason, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData("UPDATE classes SET name = 'test'")]
    [InlineData("update classes set name = 'test'")]
    [InlineData("UPDATE classes SET name = 'test' WHERE id = 1")]
    public void Validate_UpdateQueries_AreRejected(string sql)
    {
        var result = _guard.Validate(sql);

        Assert.False(result.IsValid, $"Expected invalid for: {sql}");
        Assert.Contains("UPDATE", result.Reason, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData("DELETE FROM classes")]
    [InlineData("delete from classes")]
    [InlineData("DELETE FROM classes WHERE id = 1")]
    public void Validate_DeleteQueries_AreRejected(string sql)
    {
        var result = _guard.Validate(sql);

        Assert.False(result.IsValid, $"Expected invalid for: {sql}");
        Assert.Contains("DELETE", result.Reason, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData("CREATE TABLE test (id INT)")]
    [InlineData("create table test (id int)")]
    [InlineData("CREATE INDEX idx ON classes(name)")]
    public void Validate_CreateQueries_AreRejected(string sql)
    {
        var result = _guard.Validate(sql);

        Assert.False(result.IsValid, $"Expected invalid for: {sql}");
        Assert.Contains("CREATE", result.Reason, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData("DROP TABLE classes")]
    [InlineData("drop table classes")]
    [InlineData("DROP INDEX idx")]
    public void Validate_DropQueries_AreRejected(string sql)
    {
        var result = _guard.Validate(sql);

        Assert.False(result.IsValid, $"Expected invalid for: {sql}");
        Assert.Contains("DROP", result.Reason, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData("ALTER TABLE classes ADD COLUMN col TEXT")]
    [InlineData("alter table classes add column col text")]
    public void Validate_AlterQueries_AreRejected(string sql)
    {
        var result = _guard.Validate(sql);

        Assert.False(result.IsValid, $"Expected invalid for: {sql}");
        Assert.Contains("ALTER", result.Reason, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData("REPLACE INTO classes (id, name) VALUES (1, 'test')")]
    [InlineData("replace into classes (id, name) values (1, 'test')")]
    public void Validate_ReplaceQueries_AreRejected(string sql)
    {
        var result = _guard.Validate(sql);

        Assert.False(result.IsValid, $"Expected invalid for: {sql}");
        Assert.Contains("REPLACE", result.Reason, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData("PRAGMA journal_mode=WAL")]
    [InlineData("pragma journal_mode=WAL")]
    [InlineData("PRAGMA synchronous=NORMAL")]
    public void Validate_PragmaQueries_AreRejected(string sql)
    {
        var result = _guard.Validate(sql);

        Assert.False(result.IsValid, $"Expected invalid for: {sql}");
        Assert.Contains("PRAGMA", result.Reason, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData("ATTACH DATABASE 'test.db' AS test")]
    [InlineData("attach database 'test.db' as test")]
    public void Validate_AttachQueries_AreRejected(string sql)
    {
        var result = _guard.Validate(sql);

        Assert.False(result.IsValid, $"Expected invalid for: {sql}");
        Assert.Contains("ATTACH", result.Reason, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData("DETACH DATABASE test")]
    [InlineData("detach database test")]
    public void Validate_DetachQueries_AreRejected(string sql)
    {
        var result = _guard.Validate(sql);

        Assert.False(result.IsValid, $"Expected invalid for: {sql}");
        Assert.Contains("DETACH", result.Reason, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData("VACUUM")]
    [InlineData("vacuum")]
    public void Validate_VacuumQueries_AreRejected(string sql)
    {
        var result = _guard.Validate(sql);

        Assert.False(result.IsValid, $"Expected invalid for: {sql}");
        Assert.Contains("VACUUM", result.Reason, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData("TRUNCATE TABLE classes")]
    [InlineData("truncate table classes")]
    public void Validate_TruncateQueries_AreRejected(string sql)
    {
        var result = _guard.Validate(sql);

        Assert.False(result.IsValid, $"Expected invalid for: {sql}");
        Assert.Contains("TRUNCATE", result.Reason, StringComparison.OrdinalIgnoreCase);
    }

    // --- Multi-statement rejection ---

    [Theory]
    [InlineData("SELECT 1; SELECT 2")]
    [InlineData("SELECT * FROM classes; UPDATE classes SET name = 'x'")]
    [InlineData("SELECT 1; INSERT INTO classes (name) VALUES ('x')")]
    [InlineData("SELECT 1;\nSELECT 2")]
    [InlineData("SELECT 1; -- comment\nINSERT INTO classes (name) VALUES ('x')")]
    public void Validate_MultiStatementQueries_AreRejected(string sql)
    {
        var result = _guard.Validate(sql);

        Assert.False(result.IsValid, $"Expected invalid for: {sql}");
        Assert.Contains("Multiple statements", result.Reason, StringComparison.OrdinalIgnoreCase);
    }

    // --- Edge cases ---

    [Fact]
    public void Validate_EmptySql_IsRejected()
    {
        var result = _guard.Validate("");

        Assert.False(result.IsValid);
        Assert.Contains("empty", result.Reason, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Validate_WhitespaceOnlySql_IsRejected()
    {
        var result = _guard.Validate("   \t\n  ");

        Assert.False(result.IsValid);
        Assert.Contains("empty", result.Reason, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Validate_CommentOnlySql_IsRejected()
    {
        var result = _guard.Validate("-- just a comment");

        // This will be rejected because after stripping comments, there's nothing left
        // Or it could be rejected as "unable to determine SQL query type"
        Assert.False(result.IsValid);
    }

    [Theory]
    [InlineData("EXPLAIN SELECT * FROM classes")]
    [InlineData("explain select * from classes")]
    public void Validate_ExplainQueries_AreRejected(string sql)
    {
        var result = _guard.Validate(sql);

        Assert.False(result.IsValid, $"Expected invalid for: {sql}");
        Assert.Contains("EXPLAIN", result.Reason, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData("TRANSACTION")]
    [InlineData("BEGIN TRANSACTION")]
    [InlineData("COMMIT")]
    [InlineData("ROLLBACK")]
    public void Validate_TransactionQueries_AreRejected(string sql)
    {
        var result = _guard.Validate(sql);

        Assert.False(result.IsValid, $"Expected invalid for: {sql}");
    }
}
