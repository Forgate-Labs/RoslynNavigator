using RoslynNavigator.Models;
using RoslynNavigator.Services;

namespace RoslynNavigator.Commands;

public static class FileCommitCommand
{
    public static async Task<FileCommitResult> ExecuteAsync()
    {
        var store = FilePlanStore.CreateDefault();
        var state = await store.LoadAsync();

        if (state.Operations.Count == 0)
            return new FileCommitResult { UnifiedDiff = "(nothing to commit)", BackupPath = "", FilesModified = 0 };

        var backup = BackupService.CreateDefault();
        var workingDir = Directory.GetCurrentDirectory();

        var applyResult = await new FilePlanEngine().CommitAsync(state.Operations, workingDir, backup);

        var distinctFiles = state.Operations.Select(o => o.FilePath).Distinct().Count();
        state.Operations.Clear();
        state.LastBackupPath = applyResult.BackupPath;
        await store.SaveAsync(state);

        return new FileCommitResult
        {
            UnifiedDiff = applyResult.UnifiedDiff,
            BackupPath = applyResult.BackupPath,
            FilesModified = distinctFiles
        };
    }
}
