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
