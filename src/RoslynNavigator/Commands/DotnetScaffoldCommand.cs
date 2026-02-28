using RoslynNavigator.Models;

namespace RoslynNavigator.Commands;

public static class DotnetScaffoldCommand
{
    public static async Task<DotnetScaffoldResult> ExecuteAsync(
        string path,
        string ns,
        string typeName,
        string scaffoldType)
    {
        var content = scaffoldType switch
        {
            "class"     => $"namespace {ns};\n\npublic class {typeName}\n{{\n}}\n",
            "interface" => $"namespace {ns};\n\npublic interface {typeName}\n{{\n}}\n",
            "record"    => $"namespace {ns};\n\npublic record {typeName}\n{{\n}}\n",
            "enum"      => $"namespace {ns};\n\npublic enum {typeName}\n{{\n}}\n",
            _ => throw new ArgumentException($"Unknown scaffold type: {scaffoldType}. Must be class, interface, record, or enum.")
        };

        var absPath = Path.IsPathRooted(path)
            ? path
            : Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), path));

        var dir = Path.GetDirectoryName(absPath);
        if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            Directory.CreateDirectory(dir);

        await File.WriteAllTextAsync(absPath, content);

        return new DotnetScaffoldResult
        {
            Operation = $"scaffold-{scaffoldType}",
            FilePath = path,
            TypeName = typeName,
            Namespace = ns,
            Applied = true
        };
    }
}
