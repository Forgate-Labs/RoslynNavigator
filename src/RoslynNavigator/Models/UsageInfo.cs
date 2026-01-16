namespace RoslynNavigator.Models;

public class UsageResult
{
    public required string SymbolName { get; set; }
    public required int TotalUsages { get; set; }
    public required List<UsageInfo> Usages { get; set; }
}

public class UsageInfo
{
    public required string FilePath { get; set; }
    public required int Line { get; set; }
    public required int Column { get; set; }
    public required string ContextCode { get; set; }
    public required string MethodContext { get; set; }
}
