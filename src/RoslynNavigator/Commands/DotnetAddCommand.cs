using RoslynNavigator.Models;
using RoslynNavigator.Services;

namespace RoslynNavigator.Commands;

public static class DotnetAddCommand
{
    /// <summary>
    /// Immediately adds a member (field, property, constructor, or method) to the file on disk.
    /// </summary>
    public static async Task<DotnetAddResult> ExecuteMemberAsync(
        string path, string typeName, string memberKind, string content)
    {
        var absPath = ToAbsolutePath(path);

        if (!File.Exists(absPath))
            throw new FileNotFoundException($"File not found: {absPath}");

        var sourceText = await File.ReadAllTextAsync(absPath);
        var result = DotnetAddMemberService.AddMember(sourceText, typeName, memberKind, content);

        if (!result.Success)
            throw new InvalidOperationException($"dotnet add {memberKind}: {result.Error}");

        await File.WriteAllTextAsync(absPath, result.ModifiedSource);

        return new DotnetAddResult
        {
            Operation = $"add-{memberKind}",
            FilePath = path,
            TypeName = typeName,
            MemberKind = memberKind,
            Applied = true
        };
    }

    /// <summary>
    /// Immediately adds a using directive to the file on disk. No-op if already present.
    /// </summary>
    public static async Task<DotnetAddResult> ExecuteUsingAsync(
        string path, string namespaceName)
    {
        var absPath = ToAbsolutePath(path);

        if (!File.Exists(absPath))
            throw new FileNotFoundException($"File not found: {absPath}");

        var sourceText = await File.ReadAllTextAsync(absPath);
        var result = DotnetAddMemberService.AddUsing(sourceText, namespaceName);

        if (!result.Success)
            throw new InvalidOperationException($"dotnet add using: {result.Error}");

        await File.WriteAllTextAsync(absPath, result.ModifiedSource);

        return new DotnetAddResult
        {
            Operation = "add-using",
            FilePath = path,
            TypeName = "",
            MemberKind = "using",
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
