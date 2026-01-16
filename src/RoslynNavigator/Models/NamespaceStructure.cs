namespace RoslynNavigator.Models;

public class NamespaceStructureResult
{
    public required string ProjectName { get; set; }
    public required List<NamespaceInfo> Namespaces { get; set; }
}

public class NamespaceInfo
{
    public required string Name { get; set; }
    public required int ClassCount { get; set; }
    public required List<string> Classes { get; set; }
}
