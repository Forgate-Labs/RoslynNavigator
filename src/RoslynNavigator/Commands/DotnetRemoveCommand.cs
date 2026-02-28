using RoslynNavigator.Models;
using RoslynNavigator.Services;

namespace RoslynNavigator.Commands;

public static class DotnetRemoveCommand
{
    /// <summary>
    /// Immediately removes a member (method, property, or field) from the file on disk.
    /// </summary>
    public static async Task<DotnetRemoveResult> ExecuteAsync(
        string path, string typeName, string memberKind, string memberName)
    {
        var absPath = ToAbsolutePath(path);

        if (!File.Exists(absPath))
            throw new FileNotFoundException($"File not found: {absPath}");

        var sourceText = await File.ReadAllTextAsync(absPath);
        var result = DotnetUpdateRemoveService.RemoveMember(sourceText, typeName, memberKind, memberName);

        if (!result.Success)
            throw new InvalidOperationException($"dotnet remove {memberKind}: {result.Error}");

        await File.WriteAllTextAsync(absPath, result.ModifiedSource);

        return new DotnetRemoveResult
        {
            Operation = $"remove-{memberKind}",
            FilePath = path,
            TypeName = typeName,
            MemberKind = memberKind,
            MemberName = memberName,
            Applied = true
        };
    }

    private static string ToAbsolutePath(string path)
    {
        return Path.IsPathRooted(path)
            ? path
            : Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), path));
    }
}
