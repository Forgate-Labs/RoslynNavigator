using RoslynNavigator.Models;
using RoslynNavigator.Services;

namespace RoslynNavigator.Commands;

public static class FileStatusCommand
{
    public static async Task<FileStatusResult> ExecuteAsync()
    {
        var state = await FilePlanStore.CreateDefault().LoadAsync();

        if (state.Operations.Count == 0)
        {
            return new FileStatusResult
            {
                UnifiedDiff = "(no staged operations)",
                StagedOps = 0,
                Files = []
            };
        }

        var diff = new FilePlanEngine().ComputeDiff(state.Operations, Directory.GetCurrentDirectory());
        var files = state.Operations
            .Select(op => op.FilePath)
            .Distinct()
            .ToList();

        return new FileStatusResult
        {
            UnifiedDiff = diff,
            StagedOps = state.Operations.Count,
            Files = files
        };
    }
}
