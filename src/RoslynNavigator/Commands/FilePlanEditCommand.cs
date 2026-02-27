using RoslynNavigator.Models;
using RoslynNavigator.Services;

namespace RoslynNavigator.Commands;

public static class FilePlanEditCommand
{
    public static async Task ExecuteAsync(string path, int line, string newContent)
    {
        var normalizedContent = NormalizeNewContent(newContent);

        var op = new PlanOperation
        {
            Type = OperationType.Edit,
            FilePath = path,
            Line = line,
            NewContent = normalizedContent
        };

        var errors = await new FilePlanEngine().ValidateAsync([op], Directory.GetCurrentDirectory());
        if (errors.Count > 0)
            throw new InvalidOperationException(errors[0]);

        var store = FilePlanStore.CreateDefault();
        var state = await store.LoadAsync();
        state.Operations.Add(op);
        await store.SaveAsync(state);
    }

    private static string NormalizeNewContent(string value)
    {
        return value
            .Replace("\\r\\n", "\n", StringComparison.Ordinal)
            .Replace("\\n", "\n", StringComparison.Ordinal);
    }
}
