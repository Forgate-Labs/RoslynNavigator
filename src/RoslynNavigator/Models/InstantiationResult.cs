namespace RoslynNavigator.Models;

public class InstantiationResult
{
    public required string ClassName { get; set; }
    public required List<InstantiationInfo> Instantiations { get; set; }
    public required int TotalCount { get; set; }
}

public class InstantiationInfo
{
    public required string FilePath { get; set; }
    public required int Line { get; set; }
    public required string ContainingMethod { get; set; }
    public required string ContainingClass { get; set; }
    public required string ContextCode { get; set; }
}
