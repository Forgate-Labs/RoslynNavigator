namespace RoslynNavigator.Models;

public record DotnetScaffoldResult
{
    public required string Operation { get; init; }   // "scaffold-class" | "scaffold-interface" | "scaffold-record" | "scaffold-enum"
    public required string FilePath { get; init; }
    public required string TypeName { get; init; }
    public required string Namespace { get; init; }
    public bool Applied { get; init; }
}
