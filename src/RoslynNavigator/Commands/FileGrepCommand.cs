using System.Text.RegularExpressions;
using RoslynNavigator.Models;

namespace RoslynNavigator.Commands;

public static class FileGrepCommand
{
    public static async Task<FileGrepResult> ExecuteAsync(string pattern, string? searchPath, string? ext, int maxLines)
    {
        Regex regex;
        try
        {
            regex = new Regex(pattern, RegexOptions.Compiled | RegexOptions.IgnoreCase);
        }
        catch (RegexParseException ex)
        {
            throw new ArgumentException($"Invalid regex pattern: {ex.Message}");
        }

        var cwd = Directory.GetCurrentDirectory();
        var absoluteSearch = string.IsNullOrEmpty(searchPath)
            ? cwd
            : (Path.IsPathRooted(searchPath) ? searchPath : Path.GetFullPath(Path.Combine(cwd, searchPath)));

        string[] files;
        if (File.Exists(absoluteSearch))
        {
            files = new[] { absoluteSearch };
        }
        else if (Directory.Exists(absoluteSearch))
        {
            files = Directory.GetFiles(absoluteSearch, "*", SearchOption.AllDirectories);
            if (!string.IsNullOrEmpty(ext))
            {
                files = files
                    .Where(f => Path.GetExtension(f).Equals(ext, StringComparison.OrdinalIgnoreCase))
                    .ToArray();
            }
        }
        else
        {
            throw new DirectoryNotFoundException($"Path not found: {searchPath}");
        }

        var matches = new List<GrepMatch>();
        var truncated = false;

        foreach (var file in files)
        {
            if (truncated) break;

            var fileLines = await File.ReadAllLinesAsync(file);
            var relativePath = Path.GetRelativePath(cwd, file);

            for (int i = 0; i < fileLines.Length; i++)
            {
                if (regex.IsMatch(fileLines[i]))
                {
                    matches.Add(new GrepMatch
                    {
                        FilePath = relativePath,
                        Line = i + 1,
                        Content = fileLines[i].Trim()
                    });

                    if (matches.Count >= maxLines)
                    {
                        truncated = true;
                        break;
                    }
                }
            }
        }

        return new FileGrepResult
        {
            Pattern = pattern,
            TotalMatches = matches.Count,
            Truncated = truncated,
            Matches = matches
        };
    }
}
