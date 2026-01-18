namespace RoslynNavigator.Models;

public class CallersResult
{
    public required string Symbol { get; set; }
    public required List<CallerInfo> Callers { get; set; }
    public required int TotalCount { get; set; }
}

public class CallerInfo
{
    public required string CallerClass { get; set; }
    public required string CallerMethod { get; set; }
    public required string FilePath { get; set; }
    public required int Line { get; set; }
    public required string ContextCode { get; set; }
}
