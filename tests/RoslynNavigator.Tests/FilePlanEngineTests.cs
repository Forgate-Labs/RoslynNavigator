using RoslynNavigator.Models;
using RoslynNavigator.Services;

namespace RoslynNavigator.Tests;

public class FilePlanEngineTests : IDisposable
{
    private readonly string _tempDir;
    private readonly FilePlanEngine _engine;

    public FilePlanEngineTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(_tempDir);
        _engine = new FilePlanEngine();
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
            Directory.Delete(_tempDir, recursive: true);
    }

    // --- ValidateAsync: Edit op ---

    [Fact]
    public async Task ValidateAsync_EditOpLineMatches_ReturnsNoErrors()
    {
        var filePath = WriteFile("sample.cs", "line1\nline2\nline3");
        var ops = new List<PlanOperation>
        {
            new PlanOperation { Type = OperationType.Edit, FilePath = filePath, Line = 2, OldContent = "line2", NewContent = "line2_new" }
        };

        var errors = await _engine.ValidateAsync(ops, _tempDir);

        Assert.Empty(errors);
    }

    [Fact]
    public async Task ValidateAsync_EditOpIgnoresOldContent_ReturnsNoErrors()
    {
        var filePath = WriteFile("sample.cs", "line1\nline2\nline3");
        var ops = new List<PlanOperation>
        {
            new PlanOperation { Type = OperationType.Edit, FilePath = filePath, Line = 2, OldContent = "WRONG", NewContent = "line2_new" }
        };

        var errors = await _engine.ValidateAsync(ops, _tempDir);

        Assert.Empty(errors);
    }

    [Fact]
    public async Task ValidateAsync_EditOpFileNotFound_ReturnsFileNotFoundError()
    {
        var ops = new List<PlanOperation>
        {
            new PlanOperation { Type = OperationType.Edit, FilePath = "/nonexistent/file.cs", Line = 1, OldContent = "anything", NewContent = "new" }
        };

        var errors = await _engine.ValidateAsync(ops, _tempDir);

        Assert.Single(errors);
        Assert.Contains("File not found", errors[0]);
    }

    [Fact]
    public async Task ValidateAsync_EditOpLineOutOfRange_ReturnsOutOfRangeError()
    {
        var filePath = WriteFile("sample.cs", "only one line");
        var ops = new List<PlanOperation>
        {
            new PlanOperation { Type = OperationType.Edit, FilePath = filePath, Line = 99, OldContent = "something", NewContent = "new" }
        };

        var errors = await _engine.ValidateAsync(ops, _tempDir);

        Assert.Single(errors);
        Assert.Contains("out of range", errors[0], StringComparison.OrdinalIgnoreCase);
        Assert.Contains("99", errors[0]);
    }

    // --- ValidateAsync: Delete op ---

    [Fact]
    public async Task ValidateAsync_DeleteOpLineMatches_ReturnsNoErrors()
    {
        var filePath = WriteFile("sample.cs", "line1\nline2\nline3");
        var ops = new List<PlanOperation>
        {
            new PlanOperation { Type = OperationType.Delete, FilePath = filePath, Line = 3, OldContent = "line3" }
        };

        var errors = await _engine.ValidateAsync(ops, _tempDir);

        Assert.Empty(errors);
    }

    [Fact]
    public async Task ValidateAsync_DeleteOpLineDoesNotMatch_ReturnsSpecificError()
    {
        var filePath = WriteFile("sample.cs", "line1\nline2\nline3");
        var ops = new List<PlanOperation>
        {
            new PlanOperation { Type = OperationType.Delete, FilePath = filePath, Line = 1, OldContent = "WRONG" }
        };

        var errors = await _engine.ValidateAsync(ops, _tempDir);

        Assert.Single(errors);
        Assert.Contains("Delete op", errors[0]);
        Assert.Contains("WRONG", errors[0]);
    }

    // --- ValidateAsync: Write and Append ops ---

    [Fact]
    public async Task ValidateAsync_WriteOpFileDoesNotExist_ReturnsNoErrors()
    {
        var ops = new List<PlanOperation>
        {
            new PlanOperation { Type = OperationType.Write, FilePath = "/some/new/file.cs", NewContent = "content" }
        };

        var errors = await _engine.ValidateAsync(ops, _tempDir);

        Assert.Empty(errors);
    }

    [Fact]
    public async Task ValidateAsync_AppendOpFileDoesNotExist_ReturnsNoErrors()
    {
        var ops = new List<PlanOperation>
        {
            new PlanOperation { Type = OperationType.Append, FilePath = "/some/new/file.cs", NewContent = "content" }
        };

        var errors = await _engine.ValidateAsync(ops, _tempDir);

        Assert.Empty(errors);
    }

    // --- CommitAsync ---

    [Fact]
    public async Task CommitAsync_AllValidOps_WritesFilesAndReturnsDiffAndBackupPath()
    {
        var filePath = WriteFile("target.cs", "line1\nline2\nline3");
        var backupService = new BackupService(_tempDir);

        var ops = new List<PlanOperation>
        {
            new PlanOperation { Type = OperationType.Edit, FilePath = filePath, Line = 2, OldContent = "line2", NewContent = "line2_modified" }
        };

        var result = await _engine.CommitAsync(ops, _tempDir, backupService);

        var writtenContent = await File.ReadAllTextAsync(filePath);
        Assert.Contains("line2_modified", writtenContent);
        Assert.NotEmpty(result.UnifiedDiff);
        Assert.NotEmpty(result.BackupPath);
        Assert.True(Directory.Exists(result.BackupPath));
    }

    [Fact]
    public async Task CommitAsync_InvalidEditRange_ThrowsAndDoesNotModifyAnyFile()
    {
        var filePath = WriteFile("target.cs", "line1\nline2\nline3");
        var originalContent = await File.ReadAllTextAsync(filePath);
        var backupService = new BackupService(_tempDir);

        var ops = new List<PlanOperation>
        {
            new PlanOperation { Type = OperationType.Edit, FilePath = filePath, Line = 3, NewContent = "new_line_3\nnew_line_4" }
        };

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _engine.CommitAsync(ops, _tempDir, backupService));

        var afterContent = await File.ReadAllTextAsync(filePath);
        Assert.Equal(originalContent, afterContent);
    }

    // --- RollbackAsync ---

    [Fact]
    public async Task RollbackAsync_ValidBackupPath_RestoresFilesToOriginalContent()
    {
        var filePath = WriteFile("restoreme.cs", "original content");
        var backupService = new BackupService(_tempDir);

        // Create backup
        var backupPath = await backupService.CreateBackupAsync(new[] { filePath });

        // Modify the file after backup
        await File.WriteAllTextAsync(filePath, "modified content");

        // Rollback
        await _engine.RollbackAsync(backupPath, _tempDir);

        var restoredContent = await File.ReadAllTextAsync(filePath);
        Assert.Equal("original content", restoredContent);
    }

    [Fact]
    public async Task RollbackAsync_MissingBackupPath_ThrowsInvalidOperationException()
    {
        var nonExistentPath = Path.Combine(_tempDir, "nonexistent-backup");

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _engine.RollbackAsync(nonExistentPath, _tempDir));

        Assert.Contains("Backup not found", ex.Message);
    }

    // --- ComputeDiff ---

    [Fact]
    public void ComputeDiff_EditOp_ReturnsUnifiedDiffWithCorrectFormat()
    {
        var filePath = WriteFile("diff_target.cs", "line1\nline2\nline3");

        var ops = new List<PlanOperation>
        {
            new PlanOperation { Type = OperationType.Edit, FilePath = filePath, Line = 2, OldContent = "line2", NewContent = "line2_changed" }
        };

        var diff = _engine.ComputeDiff(ops, _tempDir);

        Assert.Contains("---", diff);
        Assert.Contains("+++", diff);
        Assert.Contains("@@", diff);
        Assert.Contains("-line2", diff);
        Assert.Contains("+line2_changed", diff);
    }

    [Fact]
    public async Task CommitAsync_EditOpMultiLine_ReplacesSequentialLines()
    {
        var filePath = WriteFile("multi_target.cs", "line1\nline2\nline3\nline4");
        var backupService = new BackupService(_tempDir);

        var ops = new List<PlanOperation>
        {
            new PlanOperation { Type = OperationType.Edit, FilePath = filePath, Line = 2, NewContent = "A\nB" }
        };

        await _engine.CommitAsync(ops, _tempDir, backupService);

        var lines = await File.ReadAllLinesAsync(filePath);
        Assert.Equal("line1", lines[0]);
        Assert.Equal("A", lines[1]);
        Assert.Equal("B", lines[2]);
        Assert.Equal("line4", lines[3]);
    }

    // --- Helpers ---

    private string WriteFile(string relativeName, string content)
    {
        var path = Path.Combine(_tempDir, relativeName);
        var dir = Path.GetDirectoryName(path)!;
        if (!Directory.Exists(dir))
            Directory.CreateDirectory(dir);
        File.WriteAllText(path, content);
        return path;
    }
}
