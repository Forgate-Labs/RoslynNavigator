namespace RoslynNavigator.Models;

public class InterfaceConsumersResult
{
    public required string Interface { get; set; }
    public string? DefinedIn { get; set; }
    public int? DefinitionLine { get; set; }
    public required List<ImplementationInfo> Implementations { get; set; }
    public required List<InjectionInfo> Injections { get; set; }
}

public class InjectionInfo
{
    public required string ClassName { get; set; }
    public required string MemberName { get; set; }
    public required string MemberType { get; set; }
    public required string FilePath { get; set; }
    public required int Line { get; set; }
}
