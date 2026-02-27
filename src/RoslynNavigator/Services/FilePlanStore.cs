using System.Text.Json;
using System.Text.Json.Serialization;
using RoslynNavigator.Models;

namespace RoslynNavigator.Services;

public class FilePlanStore : IPlanStore
{
    private readonly string _planFile;

    private static readonly JsonSerializerOptions _options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() }
    };

    public FilePlanStore(string workingDirectory)
    {
        _planFile = Path.Combine(workingDirectory, ".roslyn-nav-plans.json");
    }

    public static FilePlanStore CreateDefault() => new FilePlanStore(Directory.GetCurrentDirectory());

    public async Task<PlanState> LoadAsync()
    {
        if (!File.Exists(_planFile))
            return new PlanState();

        var json = await File.ReadAllTextAsync(_planFile);
        return JsonSerializer.Deserialize<PlanState>(json, _options) ?? new PlanState();
    }

    public async Task SaveAsync(PlanState state)
    {
        var json = JsonSerializer.Serialize(state, _options);
        await File.WriteAllTextAsync(_planFile, json);
    }

    public Task ClearAsync()
    {
        if (File.Exists(_planFile))
            File.Delete(_planFile);

        return Task.CompletedTask;
    }
}
