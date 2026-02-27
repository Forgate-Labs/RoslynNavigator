using RoslynNavigator.Models;
using RoslynNavigator.Services;

namespace RoslynNavigator.Commands;

public static class FileClearCommand
{
    public static async Task<FileClearResult> ExecuteAsync()
    {
        var store = FilePlanStore.CreateDefault();
        await store.ClearAsync();
        return new FileClearResult { Message = "Staged operations cleared. No files were modified." };
    }
}
