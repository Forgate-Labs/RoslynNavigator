namespace RoslynNavigator.Models;

public class FeatureScenariosResult
{
    public required string Path { get; set; }
    public required List<FeatureInfo> Features { get; set; }
    public required FeatureSummary Summary { get; set; }
}

public class FeatureInfo
{
    public required string File { get; set; }
    public required string Name { get; set; }
    public required List<ScenarioInfo> Scenarios { get; set; }
}

public class ScenarioInfo
{
    public required int Line { get; set; }
    public required string Name { get; set; }
}

public class FeatureSummary
{
    public required int TotalFeatures { get; set; }
    public required int TotalScenarios { get; set; }
}
