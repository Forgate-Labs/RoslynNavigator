namespace RoslynNavigator.Models;

public class AttributeSearchResult
{
    public required string Attribute { get; set; }
    public string? Pattern { get; set; }
    public required List<AttributeMatchInfo> Matches { get; set; }
    public required int TotalCount { get; set; }
}

public class AttributeMatchInfo
{
    public required string MemberType { get; set; }
    public required string Name { get; set; }
    public required string AttributeArguments { get; set; }
    public required string FilePath { get; set; }
    public required int Line { get; set; }
    public required string ContainingClass { get; set; }
    public required string Namespace { get; set; }
}
