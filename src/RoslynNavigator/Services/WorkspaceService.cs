using System.Collections.Concurrent;
using Microsoft.Build.Locator;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.MSBuild;

namespace RoslynNavigator.Services;

public static class WorkspaceService
{
    private static readonly ConcurrentDictionary<string, (MSBuildWorkspace Workspace, Solution Solution)> _cache = new();
    private static readonly SemaphoreSlim _lock = new(1, 1);
    private static bool _msBuildRegistered = false;

    public static async Task<Solution> GetSolutionAsync(string solutionPath)
    {
        var absolutePath = Path.GetFullPath(solutionPath);

        if (_cache.TryGetValue(absolutePath, out var cached))
        {
            return cached.Solution;
        }

        await _lock.WaitAsync();
        try
        {
            // Double-check after acquiring lock
            if (_cache.TryGetValue(absolutePath, out cached))
            {
                return cached.Solution;
            }

            // Register MSBuild once
            if (!_msBuildRegistered)
            {
                MSBuildLocator.RegisterDefaults();
                _msBuildRegistered = true;
            }

            var workspace = MSBuildWorkspace.Create();
            workspace.WorkspaceFailed += (sender, e) =>
            {
                // Log workspace failures but continue processing
                if (e.Diagnostic.Kind == WorkspaceDiagnosticKind.Failure)
                {
                    Console.Error.WriteLine($"Workspace warning: {e.Diagnostic.Message}");
                }
            };

            var solution = await workspace.OpenSolutionAsync(absolutePath);
            _cache[absolutePath] = (workspace, solution);
            return solution;
        }
        finally
        {
            _lock.Release();
        }
    }

    public static async Task<Document?> FindDocumentAsync(Solution solution, string filePath, string solutionPath)
    {
        var absoluteFilePath = ResolveFilePath(filePath, solutionPath);

        foreach (var project in solution.Projects)
        {
            foreach (var document in project.Documents)
            {
                if (document.FilePath != null &&
                    Path.GetFullPath(document.FilePath).Equals(absoluteFilePath, StringComparison.OrdinalIgnoreCase))
                {
                    return document;
                }
            }
        }

        // Fallback: search by filename
        var fileName = Path.GetFileName(filePath);
        foreach (var project in solution.Projects)
        {
            foreach (var document in project.Documents)
            {
                if (document.Name.Equals(fileName, StringComparison.OrdinalIgnoreCase))
                {
                    return document;
                }
            }
        }

        return null;
    }

    public static Project? FindProject(Solution solution, string projectName)
    {
        return solution.Projects.FirstOrDefault(p =>
            p.Name.Equals(projectName, StringComparison.OrdinalIgnoreCase) ||
            Path.GetFileNameWithoutExtension(p.FilePath ?? "").Equals(projectName, StringComparison.OrdinalIgnoreCase));
    }

    private static string ResolveFilePath(string filePath, string solutionPath)
    {
        // 1. Try as absolute path
        if (Path.IsPathRooted(filePath) && File.Exists(filePath))
        {
            return Path.GetFullPath(filePath);
        }

        // 2. Try relative to solution directory
        var solutionDir = Path.GetDirectoryName(Path.GetFullPath(solutionPath)) ?? ".";
        var relativeToSolution = Path.Combine(solutionDir, filePath);
        if (File.Exists(relativeToSolution))
        {
            return Path.GetFullPath(relativeToSolution);
        }

        // 3. Try relative to current directory
        var relativeToCurrentDir = Path.GetFullPath(filePath);
        if (File.Exists(relativeToCurrentDir))
        {
            return relativeToCurrentDir;
        }

        // 4. Return the original path (will fallback to filename search)
        return Path.GetFullPath(filePath);
    }

    public static string GetRelativePath(string absolutePath, string solutionPath)
    {
        var solutionDir = Path.GetDirectoryName(Path.GetFullPath(solutionPath)) ?? ".";
        return Path.GetRelativePath(solutionDir, absolutePath);
    }
}
