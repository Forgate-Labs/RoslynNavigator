namespace RoslynNavigator.Models;

public class ConstructorDepsResult
{
    public required string ClassName { get; set; }
    public required string FilePath { get; set; }
    public required string Namespace { get; set; }
    public required List<ConstructorInfo> Constructors { get; set; }
}

public class ConstructorInfo
{
    public required List<ConstructorParameterInfo> Parameters { get; set; }
    public required int[] LineRange { get; set; }
    public required string Signature { get; set; }
}

public class ConstructorParameterInfo
{
    public required string Name { get; set; }
    public required string Type { get; set; }
    public required string FullTypeName { get; set; }
}
