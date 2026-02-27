namespace RoslynNavigator.Models;

public record DotnetAddResult
{
    public required string Operation { get; init; }   // "add-field" | "add-property" | "add-constructor" | "add-method" | "add-using"
    public required string FilePath { get; init; }
    public required string TypeName { get; init; }    // class/record/struct name (empty string for using)
    public required string MemberKind { get; init; }  // "field" | "property" | "constructor" | "method" | "using"
    public int TotalStagedOps { get; init; }
}
