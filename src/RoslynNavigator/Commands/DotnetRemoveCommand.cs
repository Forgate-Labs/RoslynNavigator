using System.Text.Json;
using RoslynNavigator.Models;
using RoslynNavigator.Services;

namespace RoslynNavigator.Commands;

public static class DotnetRemoveCommand
{
    private static readonly JsonSerializerOptions _metaOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    /// <summary>
    /// Stages a RemoveMember operation for a method, property, or field.
    /// </summary>
    public static async Task<DotnetRemoveResult> ExecuteAsync(
        string path, string typeName, string memberKind, string memberName)
    {
        var metadata = JsonSerializer.Serialize(
            new { typeName, memberKind, memberName },
            _metaOptions);

        var op = new PlanOperation
        {
            Type = OperationType.RemoveMember,
            FilePath = path,
            Metadata = metadata
        };

        var store = FilePlanStore.CreateDefault();
        var state = await store.LoadAsync();
        state.Operations.Add(op);
        await store.SaveAsync(state);

        return new DotnetRemoveResult
        {
            Operation = $"remove-{memberKind}",
            FilePath = path,
            TypeName = typeName,
            MemberKind = memberKind,
            MemberName = memberName,
            TotalStagedOps = state.Operations.Count
        };
    }
}
