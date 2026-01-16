namespace RoslynNavigator.Models;

public class SymbolSearchResult
{
    public required string SymbolName { get; set; }
    public required string Kind { get; set; }
    public required List<SymbolLocation> Results { get; set; }
}

public class SymbolLocation
{
    public required string FilePath { get; set; }
    public required int[] LineRange { get; set; }
    public required string Namespace { get; set; }
    public required string FullName { get; set; }
}
