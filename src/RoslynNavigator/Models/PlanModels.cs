using System.Text.Json.Serialization;

namespace RoslynNavigator.Models;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum OperationType
{
    Edit,
    Write,
    Append,
    Delete
}

public record PlanOperation
{
    public required OperationType Type { get; init; }
    public required string FilePath { get; init; }
    public int? Line { get; init; }
    public string? OldContent { get; init; }
    public string? NewContent { get; init; }
    public string? Metadata { get; init; }
}

public class PlanState
{
    public List<PlanOperation> Operations { get; set; } = new();
    public string? LastBackupPath { get; set; }
}
