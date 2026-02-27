namespace RoslynNavigator.Models;

public class FileLineInfo
{
    public int Line { get; set; }       // 1-based line number
    public string Content { get; set; } = "";
}

public class FileReadResult
{
    public string FilePath { get; set; } = "";
    public int TotalLines { get; set; }
    public int? RangeStart { get; set; }  // null if no --lines filter
    public int? RangeEnd { get; set; }    // null if no --lines filter
    public List<FileLineInfo> Lines { get; set; } = new();
}
