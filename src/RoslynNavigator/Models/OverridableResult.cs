namespace RoslynNavigator.Models;

public class OverridableResult
{
    public required string ClassName { get; set; }
    public required string MethodName { get; set; }
    public required bool IsVirtual { get; set; }
    public required bool IsOverride { get; set; }
    public required bool IsAbstract { get; set; }
    public required bool IsSealed { get; set; }
    public required bool CanBeOverridden { get; set; }
    public string? BaseMethod { get; set; }
    public required string FilePath { get; set; }
    public required int Line { get; set; }
}
