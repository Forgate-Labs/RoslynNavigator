using RoslynNavigator.Models;

namespace RoslynNavigator.Commands;

public static class FileReadCommand
{
    public static async Task<FileReadResult> ExecuteAsync(string path, string? lines)
    {
        var absolutePath = Path.IsPathRooted(path)
            ? path
            : Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), path));

        if (!File.Exists(absolutePath))
            throw new FileNotFoundException($"File not found: {path}");

        var allLines = await File.ReadAllLinesAsync(absolutePath);
        var totalLines = allLines.Length;

        int rangeStart;
        int rangeEnd;

        if (string.IsNullOrEmpty(lines))
        {
            rangeStart = 1;
            rangeEnd = totalLines;
        }
        else
        {
            var parts = lines.Split('-');
            if (parts.Length != 2 || !int.TryParse(parts[0], out rangeStart) || !int.TryParse(parts[1], out rangeEnd))
                throw new ArgumentException($"Invalid line range format: '{lines}'. Expected format: START-END (e.g., 10-20)");

            rangeStart = Math.Max(1, rangeStart);
            rangeEnd = Math.Min(totalLines, rangeEnd);

            if (rangeStart > rangeEnd)
                throw new ArgumentException("Invalid line range: start must be <= end");
        }

        var sliced = allLines[(rangeStart - 1)..rangeEnd];
        var compactLines = sliced
            .Select((line, i) => $"{rangeStart + i}: {line}")
            .ToList();

        return new FileReadResult
        {
            Lines = compactLines
        };
    }
}
