namespace RoslynNavigator.Models;

public class MethodsResult
{
    public required string ClassName { get; set; }
    public required string FilePath { get; set; }
    public required List<MethodInfo> Methods { get; set; }
}

public class MethodInfo
{
    public required string Name { get; set; }
    public required string Signature { get; set; }
    public required int[] LineRange { get; set; }
    public required string SourceCode { get; set; }
    public required string ReturnType { get; set; }
    public required List<ParameterInfo> Parameters { get; set; }
    public required string Accessibility { get; set; }
    public required bool IsAsync { get; set; }
}
