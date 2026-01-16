namespace RoslynNavigator.Models;

public class ClassListResult
{
    public required string Namespace { get; set; }
    public required int TotalClasses { get; set; }
    public required List<ClassInfo> Classes { get; set; }
}

public class ClassInfo
{
    public required string Name { get; set; }
    public required string FilePath { get; set; }
    public required int[] LineRange { get; set; }
    public required string Accessibility { get; set; }
    public required bool IsStatic { get; set; }
}
