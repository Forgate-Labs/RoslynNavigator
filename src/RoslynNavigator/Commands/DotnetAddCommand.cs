using System.Text.Json;
using RoslynNavigator.Models;
using RoslynNavigator.Services;

namespace RoslynNavigator.Commands;

public static class DotnetAddCommand
{
    private static readonly JsonSerializerOptions _metaOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    /// <summary>
    /// Stages an AddMember operation for a field, property, constructor, or method.
    /// </summary>
    public static async Task<DotnetAddResult> ExecuteMemberAsync(
        string path, string typeName, string memberKind, string content)
    {
        var metadata = JsonSerializer.Serialize(
            new { typeName, memberKind, content },
            _metaOptions);

        var op = new PlanOperation
        {
            Type = OperationType.AddMember,
            FilePath = path,
            Metadata = metadata
        };

        var store = FilePlanStore.CreateDefault();
        var state = await store.LoadAsync();
        state.Operations.Add(op);
        await store.SaveAsync(state);

        return new DotnetAddResult
        {
            Operation = $"add-{memberKind}",
            FilePath = path,
            TypeName = typeName,
            MemberKind = memberKind,
            TotalStagedOps = state.Operations.Count
        };
    }

    /// <summary>
    /// Stages an AddMember operation for a using directive.
    /// </summary>
    public static async Task<DotnetAddResult> ExecuteUsingAsync(
        string path, string namespaceName)
    {
        var typeName = "";
        var memberKind = "using";
        var content = namespaceName;

        var metadata = JsonSerializer.Serialize(
            new { typeName, memberKind, content },
            _metaOptions);

        var op = new PlanOperation
        {
            Type = OperationType.AddMember,
            FilePath = path,
            Metadata = metadata
        };

        var store = FilePlanStore.CreateDefault();
        var state = await store.LoadAsync();
        state.Operations.Add(op);
        await store.SaveAsync(state);

        return new DotnetAddResult
        {
            Operation = "add-using",
            FilePath = path,
            TypeName = "",
            MemberKind = "using",
            TotalStagedOps = state.Operations.Count
        };
    }
}
