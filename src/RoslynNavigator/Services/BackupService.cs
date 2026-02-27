namespace RoslynNavigator.Services;

public class BackupService
{
    private readonly string _workingDirectory;

    public BackupService(string workingDirectory)
    {
        _workingDirectory = workingDirectory;
    }

    public static BackupService CreateDefault() => new BackupService(Directory.GetCurrentDirectory());

    public async Task<string> CreateBackupAsync(IEnumerable<string> filePaths)
    {
        var timestamp = DateTime.UtcNow.ToString("yyyyMMdd-HHmmss");
        var backupDir = Path.Combine(_workingDirectory, ".roslyn-nav-backup", timestamp);
        Directory.CreateDirectory(backupDir);

        foreach (var filePath in filePaths)
        {
            var absolutePath = Path.IsPathRooted(filePath)
                ? filePath
                : Path.GetFullPath(Path.Combine(_workingDirectory, filePath));

            if (!File.Exists(absolutePath))
                continue;

            var relPath = Path.GetRelativePath(_workingDirectory, absolutePath);
            var destPath = Path.Combine(backupDir, relPath);

            Directory.CreateDirectory(Path.GetDirectoryName(destPath)!);
            File.Copy(absolutePath, destPath, overwrite: true);
        }

        return await Task.FromResult(backupDir);
    }
}
