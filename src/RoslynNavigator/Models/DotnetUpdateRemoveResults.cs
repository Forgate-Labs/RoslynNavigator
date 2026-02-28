namespace RoslynNavigator.Models;

public record DotnetUpdateResult
{
    public required string Operation { get; init; }   // "update-property" | "update-field"
    public required string FilePath { get; init; }
    public required string TypeName { get; init; }
    public required string MemberKind { get; init; }  // "property" | "field"
    public required string MemberName { get; init; }
    public bool Applied { get; init; }
}

public record DotnetRemoveResult
{
    public required string Operation { get; init; }   // "remove-method" | "remove-property" | "remove-field"
    public required string FilePath { get; init; }
    public required string TypeName { get; init; }
    public required string MemberKind { get; init; }  // "method" | "property" | "field"
    public required string MemberName { get; init; }
    public bool Applied { get; init; }
}
