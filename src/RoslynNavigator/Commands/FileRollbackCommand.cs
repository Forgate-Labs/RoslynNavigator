using RoslynNavigator.Models;
using RoslynNavigator.Services;

namespace RoslynNavigator.Commands;

public static class FileRollbackCommand
{
    public static async Task<FileRollbackResult> ExecuteAsync()
    {
        var store = FilePlanStore.CreateDefault();
        var state = await store.LoadAsync();

        if (string.IsNullOrEmpty(state.LastBackupPath))
            throw new InvalidOperationException("No backup found. Run 'file commit' before attempting rollback.");

        var workingDir = Directory.GetCurrentDirectory();
        await new FilePlanEngine().RollbackAsync(state.LastBackupPath, workingDir);

        var restoredCount = Directory.Exists(state.LastBackupPath)
            ? Directory.GetFiles(state.LastBackupPath, "*", SearchOption.AllDirectories).Length
            : 0;

        return new FileRollbackResult { BackupPath = state.LastBackupPath, FilesRestored = restoredCount };
    }
}
