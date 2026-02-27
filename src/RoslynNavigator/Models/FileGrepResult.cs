namespace RoslynNavigator.Models;

public class GrepMatch
{
    public string FilePath { get; set; } = "";
    public int Line { get; set; }          // 1-based line number
    public string Content { get; set; } = "";  // the matching line text
}

public class FileGrepResult
{
    public string Pattern { get; set; } = "";
    public int TotalMatches { get; set; }
    public bool Truncated { get; set; }  // true if max-lines limit was hit
    public List<GrepMatch> Matches { get; set; } = new();
}
