using RoslynNavigator.Models;
using RoslynNavigator.Services;

namespace RoslynNavigator.Commands;

public static class FilePlanEditCommand
{
    public static async Task<FilePlanStagedResult> ExecuteAsync(
        string path, int line, string oldContent, string newContent)
    {
        var op = new PlanOperation
        {
            Type = OperationType.Edit,
            FilePath = path,
            Line = line,
            OldContent = oldContent,
            NewContent = newContent
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
            Operation = "edit",
            FilePath = path,
            Line = line,
            TotalStagedOps = state.Operations.Count
        };
    }
}
