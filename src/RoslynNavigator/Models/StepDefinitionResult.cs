namespace RoslynNavigator.Models;

public class StepDefinitionResult
{
    public required string Pattern { get; set; }
    public required List<StepDefinitionInfo> Matches { get; set; }
    public required int TotalCount { get; set; }
}

public class StepDefinitionInfo
{
    public required string Type { get; set; }
    public required string Regex { get; set; }
    public required string FilePath { get; set; }
    public required string ClassName { get; set; }
    public required string MethodName { get; set; }
    public required int StartLine { get; set; }
    public required int EndLine { get; set; }
    public required int LineCount { get; set; }
    public required string Scope { get; set; }
}
