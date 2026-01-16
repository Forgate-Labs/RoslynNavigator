namespace RoslynNavigator.Models;

public class MethodResult
{
    public required string MethodName { get; set; }
    public required string ClassName { get; set; }
    public required int[] LineRange { get; set; }
    public required string FilePath { get; set; }
    public required string Signature { get; set; }
    public required string Accessibility { get; set; }
    public required bool IsAsync { get; set; }
    public required string ReturnType { get; set; }
    public required List<ParameterInfo> Parameters { get; set; }
    public required string SourceCode { get; set; }
}
