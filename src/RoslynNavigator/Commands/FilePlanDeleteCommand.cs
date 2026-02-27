using RoslynNavigator.Models;
using RoslynNavigator.Services;

namespace RoslynNavigator.Commands;

public static class FilePlanDeleteCommand
{
    public static async Task<FilePlanStagedResult> ExecuteAsync(
        string path, int line, string oldContent)
    {
        var op = new PlanOperation
        {
            Type = OperationType.Delete,
            FilePath = path,
            Line = line,
            OldContent = oldContent
        };

        var errors = await new FilePlanEngine().ValidateAsync([op], Directory.GetCurrentDirectory());
        if (errors.Count > 0)
            throw new InvalidOperationException(errors[0]);

        var store = FilePlanStore.CreateDefault();
        var state = await store.LoadAsync();
        state.Operations.Add(op);
        await store.SaveAsync(state);

        return new FilePlanStagedResult
        {
            Operation = "delete",
            FilePath = path,
            Line = line,
            TotalStagedOps = state.Operations.Count
        };
    }
}
