using System.Text.Json;
using RoslynNavigator.Models;

namespace RoslynNavigator.Services;

public record ApplyResult(string UnifiedDiff, string BackupPath);

public class FilePlanEngine
{
    /// <summary>
    /// Validates all operations without touching any file.
    /// Returns a list of error messages; empty list means all operations are valid.
    /// </summary>
    public async Task<IReadOnlyList<string>> ValidateAsync(
        IReadOnlyList<PlanOperation> ops,
        string workingDir)
    {
        var errors = new List<string>();

        foreach (var op in ops)
        {
            // Write, Append, and ScaffoldFile ops require no pre-validation
            if (op.Type == OperationType.Write || op.Type == OperationType.Append || op.Type == OperationType.ScaffoldFile)
                continue;

            // AddMember ops validate by running the service and checking for errors
            if (op.Type == OperationType.AddMember)
            {
                var absPath = ToAbsolutePath(op.FilePath, workingDir);
                if (!File.Exists(absPath))
                {
                    errors.Add($"File not found: {absPath}");
                    continue;
                }
                var sourceText = await File.ReadAllTextAsync(absPath);
                var meta = ParseAddMemberMetadata(op.Metadata);
                bool success;
                string? error;
                if (meta.MemberKind == "using")
                {
                    var result = DotnetAddMemberService.AddUsing(sourceText, meta.Content);
                    success = result.Success;
                    error = result.Error;
                }
                else
                {
                    var result = DotnetAddMemberService.AddMember(sourceText, meta.TypeName, meta.MemberKind, meta.Content);
                    success = result.Success;
                    error = result.Error;
                }
                if (!success)
                    errors.Add($"dotnet add {meta.MemberKind} on '{absPath}': {error}");
                continue;
            }

            if (op.Type == OperationType.UpdateMember)
            {
                var absPath = ToAbsolutePath(op.FilePath, workingDir);
                if (!File.Exists(absPath))
                {
                    errors.Add($"File not found: {absPath}");
                    continue;
                }
                var sourceText = await File.ReadAllTextAsync(absPath);
                var meta = ParseUpdateRemoveMetadata(op.Metadata);
                var result = DotnetUpdateRemoveService.UpdateMember(sourceText, meta.TypeName, meta.MemberKind, meta.MemberName, meta.Content);
                if (!result.Success)
                    errors.Add($"dotnet update {meta.MemberKind} on '{absPath}': {result.Error}");
                continue;
            }

            if (op.Type == OperationType.RemoveMember)
            {
                var absPath = ToAbsolutePath(op.FilePath, workingDir);
                if (!File.Exists(absPath))
                {
                    errors.Add($"File not found: {absPath}");
                    continue;
                }
                var sourceText = await File.ReadAllTextAsync(absPath);
                var meta = ParseUpdateRemoveMetadata(op.Metadata);
                var result = DotnetUpdateRemoveService.RemoveMember(sourceText, meta.TypeName, meta.MemberKind, meta.MemberName);
                if (!result.Success)
                    errors.Add($"dotnet remove {meta.MemberKind} on '{absPath}': {result.Error}");
                continue;
            }

            // Edit and Delete ops must validate target lines
            if (op.Type == OperationType.Edit || op.Type == OperationType.Delete)
            {
                var absPath = ToAbsolutePath(op.FilePath, workingDir);

                if (!File.Exists(absPath))
                {
                    errors.Add($"File not found: {absPath}");
                    continue;
                }

                var lines = await File.ReadAllLinesAsync(absPath);
                var lineNumber = op.Line ?? 0;

                if (lineNumber < 1 || lineNumber > lines.Length)
                {
                    errors.Add($"Line {lineNumber} is out of range in '{absPath}' (file has {lines.Length} lines)");
                    continue;
                }

                if (op.Type == OperationType.Edit)
                {
                    var replacementLines = SplitContentLines(op.NewContent);
                    var endLine = lineNumber + replacementLines.Count - 1;
                    if (replacementLines.Count == 0)
                    {
                        errors.Add($"Edit op on '{absPath}' line {lineNumber}: replacement content cannot be empty");
                        continue;
                    }

                    if (endLine > lines.Length)
                    {
                        errors.Add($"Edit op on '{absPath}' lines {lineNumber}-{endLine} is out of range (file has {lines.Length} lines)");
                        continue;
                    }
                }
                else
                {
                    var actualLine = lines[lineNumber - 1];
                    var expectedOld = op.OldContent ?? string.Empty;
                    if (actualLine != expectedOld)
                    {
                        errors.Add($"Delete op on '{absPath}' line {lineNumber}: expected '{expectedOld}' but found '{actualLine}'");
                    }
                }
            }
        }

        return errors;
    }

    /// <summary>
    /// Computes a unified diff showing what CommitAsync would apply, without writing any files.
    /// </summary>
    public string ComputeDiff(IReadOnlyList<PlanOperation> ops, string workingDir)
    {
        var originalLines = LoadOriginalLines(ops, workingDir);
        var newLines = ApplyOpsInMemory(ops, workingDir, originalLines);
        return ComputeDiffFromMemory(originalLines, newLines, workingDir);
    }

    /// <summary>
    /// Validates all ops, creates a backup, applies all ops atomically, and returns the diff + backup path.
    /// Throws InvalidOperationException if any validation fails — no files are modified in that case.
    /// </summary>
    public async Task<ApplyResult> CommitAsync(
        IReadOnlyList<PlanOperation> ops,
        string workingDir,
        BackupService backupService)
    {
        // Step 1: Validate everything before touching any file
        var errors = await ValidateAsync(ops, workingDir);
        if (errors.Count > 0)
        {
            var errorList = string.Join(Environment.NewLine, errors.Select(e => $"  - {e}"));
            throw new InvalidOperationException(
                $"Cannot commit: {errors.Count} validation error(s):{Environment.NewLine}{errorList}");
        }

        // Step 2: Identify affected files and create backup
        var affectedPaths = ops
            .Select(op => ToAbsolutePath(op.FilePath, workingDir))
            .Distinct()
            .ToList();

        var backupPath = await backupService.CreateBackupAsync(affectedPaths);

        // Step 3: Compute final in-memory state (reads before any writes)
        var originalLines = LoadOriginalLines(ops, workingDir);
        var newLines = ApplyOpsInMemory(ops, workingDir, originalLines);

        // Step 4: Compute unified diff before writing (needs both original and new state)
        var diff = ComputeDiffFromMemory(originalLines, newLines, workingDir);

        // Step 5: Write all files to disk atomically (all reads already done above)
        foreach (var (filePath, lines) in newLines)
        {
            var dir = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            await File.WriteAllLinesAsync(filePath, lines);
        }

        return new ApplyResult(diff, backupPath);
    }

    /// <summary>
    /// Restores all files from the backup directory to the working directory, preserving relative paths.
    /// Throws InvalidOperationException if backupPath does not exist.
    /// </summary>
    public async Task RollbackAsync(string backupPath, string workingDir)
    {
        if (!Directory.Exists(backupPath))
            throw new InvalidOperationException($"Backup not found: {backupPath}");

        foreach (var backupFile in Directory.EnumerateFiles(backupPath, "*", SearchOption.AllDirectories))
        {
            var relPath = Path.GetRelativePath(backupPath, backupFile);
            var destPath = Path.GetFullPath(Path.Combine(workingDir, relPath));

            var destDir = Path.GetDirectoryName(destPath);
            if (!string.IsNullOrEmpty(destDir) && !Directory.Exists(destDir))
                Directory.CreateDirectory(destDir);

            File.Copy(backupFile, destPath, overwrite: true);
        }

        await Task.CompletedTask;
    }

    // ---- Private helpers ----

    /// <summary>Computes unified diff from already-loaded original and new line states.</summary>
    private static string ComputeDiffFromMemory(
        Dictionary<string, string[]> originalLines,
        Dictionary<string, List<string>> newLines,
        string workingDir)
    {
        var sb = new System.Text.StringBuilder();

        foreach (var filePath in newLines.Keys)
        {
            var original = originalLines.TryGetValue(filePath, out var orig) ? orig : Array.Empty<string>();
            var modified = newLines[filePath].ToArray();

            var diffBlock = BuildUnifiedDiff(filePath, workingDir, original, modified);
            if (!string.IsNullOrEmpty(diffBlock))
                sb.AppendLine(diffBlock);
        }

        return sb.ToString();
    }

    private static string ToAbsolutePath(string filePath, string workingDir)
    {
        return Path.IsPathRooted(filePath)
            ? filePath
            : Path.GetFullPath(Path.Combine(workingDir, filePath));
    }

    /// <summary>Reads original lines for all files referenced by ops that require existing content.</summary>
    private static Dictionary<string, string[]> LoadOriginalLines(
        IReadOnlyList<PlanOperation> ops,
        string workingDir)
    {
        var result = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase);

        foreach (var op in ops)
        {
            var absPath = ToAbsolutePath(op.FilePath, workingDir);
            if (!result.ContainsKey(absPath))
            {
                result[absPath] = File.Exists(absPath)
                    ? File.ReadAllLines(absPath)
                    : Array.Empty<string>();
            }
        }

        return result;
    }

    /// <summary>
    /// Applies all ops in order to in-memory copies of the files.
    /// Returns a dictionary of absolute file path → new lines.
    /// </summary>
    private static Dictionary<string, List<string>> ApplyOpsInMemory(
        IReadOnlyList<PlanOperation> ops,
        string workingDir,
        Dictionary<string, string[]> originalLines)
    {
        // Start with mutable copies of original lines
        var state = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);

        foreach (var (path, lines) in originalLines)
            state[path] = new List<string>(lines);

        foreach (var op in ops)
        {
            var absPath = ToAbsolutePath(op.FilePath, workingDir);

            if (!state.ContainsKey(absPath))
                state[absPath] = new List<string>();

            var fileLines = state[absPath];

            switch (op.Type)
            {
                case OperationType.Edit:
                    if (op.Line.HasValue && op.Line.Value >= 1 && op.Line.Value <= fileLines.Count)
                    {
                        var replacementLines = SplitContentLines(op.NewContent);
                        var start = op.Line.Value - 1;
                        var maxReplace = Math.Min(replacementLines.Count, fileLines.Count - start);
                        for (var i = 0; i < maxReplace; i++)
                        {
                            fileLines[start + i] = replacementLines[i];
                        }
                    }
                    break;

                case OperationType.Delete:
                    if (op.Line.HasValue && op.Line.Value >= 1 && op.Line.Value <= fileLines.Count)
                        fileLines.RemoveAt(op.Line.Value - 1);
                    break;

                case OperationType.Write:
                    fileLines.Clear();
                    if (op.NewContent != null)
                    {
                        var writeLines = op.NewContent.Split('\n');
                        fileLines.AddRange(writeLines.Select(l => l.TrimEnd('\r')));
                    }
                    break;

                case OperationType.Append:
                    if (op.NewContent != null)
                    {
                        var appendLines = op.NewContent.Split('\n');
                        fileLines.AddRange(appendLines.Select(l => l.TrimEnd('\r')));
                    }
                    break;

                case OperationType.ScaffoldFile:
                    fileLines.Clear();
                    if (op.NewContent != null)
                    {
                        var scaffoldLines = op.NewContent.Split('\n');
                        fileLines.AddRange(scaffoldLines.Select(l => l.TrimEnd('\r')));
                    }
                    break;

                case OperationType.AddMember:
                {
                    var sourceText = string.Join("\n", fileLines);
                    var meta = ParseAddMemberMetadata(op.Metadata);
                    string modifiedSource = meta.MemberKind == "using"
                        ? DotnetAddMemberService.AddUsing(sourceText, meta.Content).ModifiedSource
                        : DotnetAddMemberService.AddMember(sourceText, meta.TypeName, meta.MemberKind, meta.Content).ModifiedSource;
                    fileLines.Clear();
                    fileLines.AddRange(modifiedSource.Split('\n').Select(l => l.TrimEnd('\r')));
                    break;
                }

                case OperationType.UpdateMember:
                {
                    var sourceText = string.Join("\n", fileLines);
                    var meta = ParseUpdateRemoveMetadata(op.Metadata);
                    var modifiedSource = DotnetUpdateRemoveService.UpdateMember(sourceText, meta.TypeName, meta.MemberKind, meta.MemberName, meta.Content).ModifiedSource;
                    fileLines.Clear();
                    fileLines.AddRange(modifiedSource.Split('\n').Select(l => l.TrimEnd('\r')));
                    break;
                }

                case OperationType.RemoveMember:
                {
                    var sourceText = string.Join("\n", fileLines);
                    var meta = ParseUpdateRemoveMetadata(op.Metadata);
                    var modifiedSource = DotnetUpdateRemoveService.RemoveMember(sourceText, meta.TypeName, meta.MemberKind, meta.MemberName).ModifiedSource;
                    fileLines.Clear();
                    fileLines.AddRange(modifiedSource.Split('\n').Select(l => l.TrimEnd('\r')));
                    break;
                }
            }
        }

        return state;
    }

    /// <summary>Produces a unified diff string for a single file.</summary>
    private static string BuildUnifiedDiff(
        string absPath,
        string workingDir,
        string[] original,
        string[] modified)
    {
        if (original.SequenceEqual(modified))
            return string.Empty;

        var relPath = Path.IsPathRooted(absPath) && workingDir.Length > 0
            ? Path.GetRelativePath(workingDir, absPath)
            : absPath;

        var hunks = ComputeHunks(original, modified, contextLines: 3);

        if (hunks.Count == 0)
            return string.Empty;

        var sb = new System.Text.StringBuilder();
        sb.AppendLine($"--- a/{relPath}");
        sb.AppendLine($"+++ b/{relPath}");

        foreach (var hunk in hunks)
            sb.Append(hunk);

        return sb.ToString();
    }

    private static List<string> ComputeHunks(string[] original, string[] modified, int contextLines)
    {
        // Build a simple edit script using LCS
        var editScript = BuildEditScript(original, modified);

        // Group edits into hunks with context
        var hunks = new List<string>();
        int i = 0;
        while (i < editScript.Count)
        {
            // Skip context lines
            if (editScript[i].Kind == EditKind.Equal)
            {
                i++;
                continue;
            }

            // Found a change — expand context around it
            int hunkStart = i;
            while (hunkStart > 0 && editScript[hunkStart - 1].Kind == EditKind.Equal &&
                   i - hunkStart < contextLines)
            {
                hunkStart--;
            }

            // Advance to end of change region + context
            int hunkEnd = i;
            while (hunkEnd < editScript.Count)
            {
                if (editScript[hunkEnd].Kind != EditKind.Equal)
                {
                    hunkEnd++;
                }
                else
                {
                    // Count how many equal lines follow
                    int eq = 0;
                    int j = hunkEnd;
                    while (j < editScript.Count && editScript[j].Kind == EditKind.Equal)
                    {
                        eq++;
                        j++;
                    }

                    if (eq > contextLines * 2 || j >= editScript.Count)
                    {
                        hunkEnd = Math.Min(hunkEnd + contextLines, editScript.Count);
                        break;
                    }
                    else
                    {
                        hunkEnd = j;
                    }
                }
            }

            // Build hunk header and lines
            var hunkEdits = editScript.GetRange(hunkStart, hunkEnd - hunkStart);

            int origStart = hunkEdits.Where(e => e.Kind != EditKind.Insert).Sum(_ => 1) == 0
                ? 0
                : hunkEdits[0].OrigLineNo;

            int newStart = hunkEdits[0].NewLineNo;
            int origCount = hunkEdits.Count(e => e.Kind != EditKind.Insert);
            int newCount = hunkEdits.Count(e => e.Kind != EditKind.Delete);

            var hunkSb = new System.Text.StringBuilder();
            hunkSb.AppendLine($"@@ -{origStart},{origCount} +{newStart},{newCount} @@");

            foreach (var edit in hunkEdits)
            {
                switch (edit.Kind)
                {
                    case EditKind.Equal:
                        hunkSb.AppendLine($" {edit.Content}");
                        break;
                    case EditKind.Delete:
                        hunkSb.AppendLine($"-{edit.Content}");
                        break;
                    case EditKind.Insert:
                        hunkSb.AppendLine($"+{edit.Content}");
                        break;
                }
            }

            hunks.Add(hunkSb.ToString());
            i = hunkEnd;
        }

        return hunks;
    }

    private enum EditKind { Equal, Insert, Delete }

    private record EditEntry(EditKind Kind, string Content, int OrigLineNo, int NewLineNo);

    private static (string TypeName, string MemberKind, string Content) ParseAddMemberMetadata(string? metadata)
    {
        if (string.IsNullOrEmpty(metadata))
            throw new InvalidOperationException("AddMember op is missing Metadata JSON");
        using var doc = System.Text.Json.JsonDocument.Parse(metadata);
        var root = doc.RootElement;
        return (
            root.GetProperty("typeName").GetString() ?? "",
            root.GetProperty("memberKind").GetString() ?? "",
            root.GetProperty("content").GetString() ?? ""
        );
    }

    private static (string TypeName, string MemberKind, string MemberName, string Content) ParseUpdateRemoveMetadata(string? metadata)
    {
        if (string.IsNullOrEmpty(metadata))
            return (string.Empty, string.Empty, string.Empty, string.Empty);
        var doc = JsonSerializer.Deserialize<JsonElement>(metadata);
        var typeName = doc.TryGetProperty("typeName", out var tn) ? tn.GetString() ?? "" : "";
        var memberKind = doc.TryGetProperty("memberKind", out var mk) ? mk.GetString() ?? "" : "";
        var memberName = doc.TryGetProperty("memberName", out var mn) ? mn.GetString() ?? "" : "";
        var content = doc.TryGetProperty("content", out var c) ? c.GetString() ?? "" : "";
        return (typeName, memberKind, memberName, content);
    }

    private static List<string> SplitContentLines(string? content)
    {
        if (content == null)
        {
            return new List<string>();
        }

        return content
            .Split('\n')
            .Select(line => line.TrimEnd('\r'))
            .ToList();
    }

    private static List<EditEntry> BuildEditScript(string[] original, string[] modified)
    {
        // Simple LCS-based diff
        int m = original.Length;
        int n = modified.Length;

        // Build LCS table
        var dp = new int[m + 1, n + 1];
        for (int i = m - 1; i >= 0; i--)
        {
            for (int j = n - 1; j >= 0; j--)
            {
                dp[i, j] = original[i] == modified[j]
                    ? dp[i + 1, j + 1] + 1
                    : Math.Max(dp[i + 1, j], dp[i, j + 1]);
            }
        }

        // Trace back through the table to build edit script
        var result = new List<EditEntry>();
        int oi = 0, ni = 0;

        while (oi < m || ni < n)
        {
            if (oi < m && ni < n && original[oi] == modified[ni])
            {
                result.Add(new EditEntry(EditKind.Equal, original[oi], oi + 1, ni + 1));
                oi++;
                ni++;
            }
            else if (ni < n && (oi >= m || dp[oi, ni + 1] >= dp[oi + 1, ni]))
            {
                result.Add(new EditEntry(EditKind.Insert, modified[ni], oi + 1, ni + 1));
                ni++;
            }
            else
            {
                result.Add(new EditEntry(EditKind.Delete, original[oi], oi + 1, ni + 1));
                oi++;
            }
        }

        return result;
    }
}
