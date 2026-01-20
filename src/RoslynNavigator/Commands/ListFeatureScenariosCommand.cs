using RoslynNavigator.Models;

namespace RoslynNavigator.Commands;

public static class ListFeatureScenariosCommand
{
    // Gherkin keywords in multiple languages
    private static readonly string[] FeatureKeywords =
    {
        "Feature:", "Funcionalidade:", "Característica:", "Caracteristica:",  // EN, PT-BR, PT
        "Fonctionnalité:", "Función:", "Funcion:", "Funktionalität:",         // FR, ES, DE
        "Funzionalità:", "機能:", "기능:", "Функция:", "Функціонал:"          // IT, JA, KO, RU, UK
    };

    private static readonly string[] ScenarioKeywords =
    {
        "Scenario:", "Cenário:", "Cenario:", "Exemplo:",                      // EN, PT-BR
        "Scénario:", "Escenario:", "Szenario:", "Scenario:",                  // FR, ES, DE, IT
        "シナリオ:", "시나리오:", "Сценарий:", "Сценарій:"                     // JA, KO, RU, UK
    };

    private static readonly string[] ScenarioOutlineKeywords =
    {
        "Scenario Outline:", "Scenario Template:",                             // EN
        "Esquema do Cenário:", "Esquema do Cenario:", "Delineação do Cenário:", // PT-BR
        "Esquema del escenario:", "Plan du Scénario:", "Szenariovorlage:",     // ES, FR, DE
        "Schema dello scenario:", "シナリオアウトライン:", "시나리오 개요:",      // IT, JA, KO
        "Структура сценария:", "Структура сценарію:"                           // RU, UK
    };

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

            // Parse Feature line (multi-language)
            var featureKeyword = FindMatchingKeyword(line, FeatureKeywords);
            if (featureKeyword != null)
            {
                featureName = line.Substring(featureKeyword.Length).Trim();
                continue;
            }

            // Parse Scenario Outline (check before Scenario as it's more specific)
            var outlineKeyword = FindMatchingKeyword(line, ScenarioOutlineKeywords);
            if (outlineKeyword != null)
            {
                var scenarioName = line.Substring(outlineKeyword.Length).Trim();
                scenarios.Add(new ScenarioInfo
                {
                    Line = lineNumber,
                    Name = scenarioName
                });
                continue;
            }

            // Parse Scenario
            var scenarioKeyword = FindMatchingKeyword(line, ScenarioKeywords);
            if (scenarioKeyword != null)
            {
                var scenarioName = line.Substring(scenarioKeyword.Length).Trim();
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

    private static string? FindMatchingKeyword(string line, string[] keywords)
    {
        foreach (var keyword in keywords)
        {
            if (line.StartsWith(keyword, StringComparison.OrdinalIgnoreCase))
            {
                return keyword;
            }
        }
        return null;
    }
}
