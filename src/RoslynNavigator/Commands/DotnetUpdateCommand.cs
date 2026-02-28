using RoslynNavigator.Models;
using RoslynNavigator.Services;

namespace RoslynNavigator.Commands;

public static class DotnetUpdateCommand
{
    /// <summary>
    /// Immediately replaces an existing member (property or field) in the file on disk.
    /// </summary>
    public static async Task<DotnetUpdateResult> ExecuteAsync(
        string path, string typeName, string memberKind, string memberName, string newContent)
    {
        var absPath = ToAbsolutePath(path);

        if (!File.Exists(absPath))
            throw new FileNotFoundException($"File not found: {absPath}");

        var sourceText = await File.ReadAllTextAsync(absPath);
        var result = DotnetUpdateRemoveService.UpdateMember(sourceText, typeName, memberKind, memberName, newContent);

        if (!result.Success)
            throw new InvalidOperationException($"dotnet update {memberKind}: {result.Error}");

        await File.WriteAllTextAsync(absPath, result.ModifiedSource);

        return new DotnetUpdateResult
        {
            Operation = $"update-{memberKind}",
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
