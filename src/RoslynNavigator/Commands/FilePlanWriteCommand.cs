using RoslynNavigator.Models;
using RoslynNavigator.Services;

namespace RoslynNavigator.Commands;

public static class FilePlanWriteCommand
{
    public static async Task<FilePlanStagedResult> ExecuteAsync(string path, string content)
    {
        var op = new PlanOperation
        {
            Type = OperationType.Write,
            FilePath = path,
            NewContent = content
        };

        var store = FilePlanStore.CreateDefault();
        var state = await store.LoadAsync();
        state.Operations.Add(op);
        await store.SaveAsync(state);

        return new FilePlanStagedResult
        {
            Operation = "write",
            FilePath = path,
            TotalStagedOps = state.Operations.Count
        };
    }
}
