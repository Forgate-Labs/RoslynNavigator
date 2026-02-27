using RoslynNavigator.Models;
using RoslynNavigator.Services;

namespace RoslynNavigator.Commands;

public static class FilePlanAppendCommand
{
    public static async Task<FilePlanStagedResult> ExecuteAsync(string path, string content)
    {
        var op = new PlanOperation
        {
            Type = OperationType.Append,
            FilePath = path,
            NewContent = content
        };

        var store = FilePlanStore.CreateDefault();
        var state = await store.LoadAsync();
        state.Operations.Add(op);
        await store.SaveAsync(state);

        return new FilePlanStagedResult
        {
            Operation = "append",
            FilePath = path,
            TotalStagedOps = state.Operations.Count
        };
    }
}
