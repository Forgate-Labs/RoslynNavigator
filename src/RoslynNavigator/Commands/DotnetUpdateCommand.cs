using System.Text.Json;
using RoslynNavigator.Models;
using RoslynNavigator.Services;

namespace RoslynNavigator.Commands;

public static class DotnetUpdateCommand
{
    private static readonly JsonSerializerOptions _metaOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    /// <summary>
    /// Stages an UpdateMember operation for a property or field.
    /// </summary>
    public static async Task<DotnetUpdateResult> ExecuteAsync(
        string path, string typeName, string memberKind, string memberName, string newContent)
    {
        var metadata = JsonSerializer.Serialize(
            new { typeName, memberKind, memberName, content = newContent },
            _metaOptions);

        var op = new PlanOperation
        {
            Type = OperationType.UpdateMember,
            FilePath = path,
            Metadata = metadata
        };

        var store = FilePlanStore.CreateDefault();
        var state = await store.LoadAsync();
        state.Operations.Add(op);
        await store.SaveAsync(state);

        return new DotnetUpdateResult
        {
            Operation = $"update-{memberKind}",
            FilePath = path,
            TypeName = typeName,
            MemberKind = memberKind,
            MemberName = memberName,
            TotalStagedOps = state.Operations.Count
        };
    }
}
