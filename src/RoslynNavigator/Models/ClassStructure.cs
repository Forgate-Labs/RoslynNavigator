namespace RoslynNavigator.Models;

public class ClassStructure
{
    public required string ClassName { get; set; }
    public required string Namespace { get; set; }
    public required int[] LineRange { get; set; }
    public required string FilePath { get; set; }
    public required List<MemberInfo> Members { get; set; }
}
