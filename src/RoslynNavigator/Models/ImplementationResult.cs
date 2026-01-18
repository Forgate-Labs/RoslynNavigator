namespace RoslynNavigator.Models;

public class ImplementationResult
{
    public required string Interface { get; set; }
    public required List<ImplementationInfo> Implementations { get; set; }
    public required int TotalCount { get; set; }
}

public class ImplementationInfo
{
    public required string Name { get; set; }
    public required string Kind { get; set; }
    public required string FilePath { get; set; }
    public required int Line { get; set; }
    public required string Namespace { get; set; }
}
