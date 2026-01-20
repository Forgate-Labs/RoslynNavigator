using RoslynNavigator.Models;

namespace RoslynNavigator.Commands;

public static class ListFeatureScenariosCommand
{
    public static Task<FeatureScenariosResult> ExecuteAsync(string path)
    {
        if (string.IsNullOrEmpty(path))
            throw new ArgumentException("Path is required");

        if (!Directory.Exists(path))
            throw new ArgumentException($"Directory not found: {path}");

        var features = new List<FeatureInfo>();
        var featureFiles = Directory.GetFiles(path, "*.feature", SearchOption.AllDirectories);

        foreach (var filePath in featureFiles)
        {
            var featureInfo = ParseFeatureFile(filePath, path);
            if (featureInfo != null)
            {
                features.Add(featureInfo);
            }
        }

        var totalScenarios = features.Sum(f => f.Scenarios.Count);

        return Task.FromResult(new FeatureScenariosResult
        {
            Path = path,
            Features = features,
            Summary = new FeatureSummary
            {
                TotalFeatures = features.Count,
                TotalScenarios = totalScenarios
            }
        });
    }

    private static FeatureInfo? ParseFeatureFile(string filePath, string basePath)
    {
        var lines = File.ReadAllLines(filePath);
        string? featureName = null;
        var scenarios = new List<ScenarioInfo>();

        for (var i = 0; i < lines.Length; i++)
        {
            var line = lines[i].Trim();
            var lineNumber = i + 1; // 1-based line numbers

            // Parse Feature line
            if (line.StartsWith("Feature:", StringComparison.OrdinalIgnoreCase))
            {
                featureName = line.Substring("Feature:".Length).Trim();
            }
            // Parse Scenario or Scenario Outline
            else if (line.StartsWith("Scenario Outline:", StringComparison.OrdinalIgnoreCase))
            {
                var scenarioName = line.Substring("Scenario Outline:".Length).Trim();
                scenarios.Add(new ScenarioInfo
                {
                    Line = lineNumber,
                    Name = scenarioName
                });
            }
            else if (line.StartsWith("Scenario:", StringComparison.OrdinalIgnoreCase))
            {
                var scenarioName = line.Substring("Scenario:".Length).Trim();
                scenarios.Add(new ScenarioInfo
                {
                    Line = lineNumber,
                    Name = scenarioName
                });
            }
        }

        // Return null if no feature found
        if (featureName == null)
            return null;

        // Get relative path
        var relativePath = Path.GetRelativePath(basePath, filePath);

        return new FeatureInfo
        {
            File = relativePath,
            Name = featureName,
            Scenarios = scenarios
        };
    }
}
