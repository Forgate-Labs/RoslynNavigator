using Microsoft.Data.Sqlite;

namespace RoslynNavigator.Services;

public class SnapshotPathService
{
    private const string SnapshotsFolder = ".roslyn-nav";
    private const string SnapshotsSubFolder = "snapshots";
    private const string DefaultDbName = "snapshot.db";

    /// <summary>
    /// Resolves the default snapshot DB path from a solution path.
    /// If solution is /path/to/MySolution.sln, returns /path/to/.roslyn-nav/snapshots/MySolution.snapshot.db
    /// </summary>
    public string ResolveDefaultPath(string? solutionPath)
    {
        if (string.IsNullOrEmpty(solutionPath))
        {
            throw new ArgumentException("Solution path cannot be null or empty", nameof(solutionPath));
        }

        var solutionDir = Path.GetDirectoryName(solutionPath) 
            ?? throw new ArgumentException("Invalid solution path - no directory", nameof(solutionPath));
        
        var solutionName = Path.GetFileNameWithoutExtension(solutionPath);
        var snapshotDir = Path.Combine(solutionDir, SnapshotsFolder, SnapshotsSubFolder);
        
        return Path.Combine(snapshotDir, $"{solutionName}.snapshot.db");
    }

    /// <summary>
    /// Creates parent directories for the given path if they don't exist.
    /// </summary>
    public void EnsureParentDirectoryExists(string dbPath)
    {
        var parentDir = Path.GetDirectoryName(dbPath);
        if (!string.IsNullOrEmpty(parentDir) && !Directory.Exists(parentDir))
        {
            Directory.CreateDirectory(parentDir);
        }
    }

    /// <summary>
    /// Normalizes a path - converts relative paths to absolute and normalizes separators.
    /// </summary>
    public string NormalizePath(string path)
    {
        if (string.IsNullOrEmpty(path))
        {
            throw new ArgumentException("Path cannot be null or empty", nameof(path));
        }

        if (!Path.IsPathRooted(path))
        {
            path = Path.GetFullPath(path);
        }

        return path.Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar);
    }

    /// <summary>
    /// Determines if the path is the default path for a given solution.
    /// </summary>
    public bool IsDefaultPath(string dbPath, string solutionPath)
    {
        var expectedDefault = ResolveDefaultPath(solutionPath);
        return string.Equals(NormalizePath(dbPath), NormalizePath(expectedDefault), StringComparison.OrdinalIgnoreCase);
    }
}
