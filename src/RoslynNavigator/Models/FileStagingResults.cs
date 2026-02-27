namespace RoslynNavigator.Models;

public record FilePlanStagedResult
{
    public required string Operation { get; init; }  // "edit", "write", "append", "delete"
    public required string FilePath { get; init; }
    public int? Line { get; init; }
    public int TotalStagedOps { get; init; }
}

public record FileStatusResult
{
    public required string UnifiedDiff { get; init; }
    public int StagedOps { get; init; }
    public required List<string> Files { get; init; }
}

public record FileCommitResult
{
    public required string UnifiedDiff { get; init; }
    public required string BackupPath { get; init; }
    public int FilesModified { get; init; }
}

public record FileRollbackResult
{
    public required string BackupPath { get; init; }
    public int FilesRestored { get; init; }
}

public record FileClearResult
{
    public required string Message { get; init; }
}
