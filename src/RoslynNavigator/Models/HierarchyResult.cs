namespace RoslynNavigator.Models;

public class HierarchyResult
{
    public required string ClassName { get; set; }
    public required string FilePath { get; set; }
    public required string Namespace { get; set; }
    public required List<string> BaseTypes { get; set; }
    public required List<string> Interfaces { get; set; }
    public required List<DerivedTypeInfo> DerivedTypes { get; set; }
}

public class DerivedTypeInfo
{
    public required string Name { get; set; }
    public required string Kind { get; set; }
    public required string FilePath { get; set; }
    public required int Line { get; set; }
    public required string Namespace { get; set; }
}
