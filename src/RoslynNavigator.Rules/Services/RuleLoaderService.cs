using System.Reflection;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;
using RoslynNavigator.Rules.Models;

namespace RoslynNavigator.Rules.Services;

/// <summary>
/// Service for loading rule definitions from builtin embedded YAML resources and optional domain YAML files.
/// </summary>
public class RuleLoaderService
{
    private const string EmbeddedResourcePrefix = "RoslynNavigator.Rules.Resources.Rules.";
    private const string DomainRulesFolderName = "roslyn-nav-rules";
    private static readonly string[] SupportedRuleExtensions = new[] { ".yaml", ".yml" };

    private readonly IDeserializer _deserializer;

    private static readonly Dictionary<string, string> YamlKeyAliases = new(StringComparer.Ordinal)
    {
        ["returns_null"] = "returnsNull",
        ["cognitive_complexity"] = "cognitiveComplexity",
        ["has_try_catch"] = "hasTryCatch",
        ["calls_external"] = "callsExternal",
        ["accesses_db"] = "accessesDb",
        ["filters_by_tenant"] = "filtersByTenant",
        ["method_should_validate_input"] = "methodShouldValidateInput",
        ["parameter_count_min"] = "parameterCountMin",
        ["uses_insecure_random"] = "usesInsecureRandom",
        ["uses_weak_crypto"] = "usesWeakCrypto",
        ["catches_general_exception"] = "catchesGeneralException",
        ["throws_general_exception"] = "throwsGeneralException",
        ["has_sql_string_concatenation"] = "hasSqlStringConcatenation",
        ["has_hardcoded_secret"] = "hasHardcodedSecret"
    };

    /// <summary>
    /// Embedded resource names that contain builtin rule packs.
    /// </summary>
    public static readonly string[] BuiltinRulePackNames = new[]
    {
        "architecture.yaml",
        "code-quality.yaml",
        "security.yaml"
    };

    public RuleLoaderService()
    {
        _deserializer = new DeserializerBuilder()
            .WithNamingConvention(CamelCaseNamingConvention.Instance)
            .IgnoreUnmatchedProperties()
            .Build();
    }

    /// <summary>
    /// Loads all rules from builtin embedded resources.
    /// </summary>
    public RuleLoadResult LoadBuiltinRules()
    {
        var result = new RuleLoadResult { Source = "builtin" };
        var assembly = Assembly.GetExecutingAssembly();

        foreach (var rulePackName in BuiltinRulePackNames)
        {
            var resourceName = EmbeddedResourcePrefix + rulePackName;
            try
            {
                using var stream = assembly.GetManifestResourceStream(resourceName);
                if (stream == null)
                {
                    result.Errors.Add(new RuleLoadError
                    {
                        File = resourceName,
                        Reason = $"Embedded resource '{resourceName}' not found in assembly"
                    });
                    continue;
                }

                using var reader = new StreamReader(stream);
                var yamlContent = reader.ReadToEnd();
                var rulePack = _deserializer.Deserialize<RulePack>(NormalizeYamlAliases(yamlContent));

                if (rulePack?.Rules != null)
                {
                    ValidateAndAddRules(rulePack.Rules, rulePackName, result);
                }
            }
            catch (Exception ex)
            {
                result.Errors.Add(new RuleLoadError
                {
                    File = resourceName,
                    Reason = $"Failed to parse YAML: {ex.Message}",
                    Exception = ex
                });
            }
        }

        result.Success = result.Errors.Count == 0;
        return result;
    }

    /// <summary>
    /// Loads rules from optional domain YAML files in the specified directory.
    /// </summary>
    /// <param name="domainRulesPath">Path to the domain rules directory (roslyn-nav-rules)</param>
    public RuleLoadResult LoadDomainRules(string domainRulesPath)
    {
        var result = new RuleLoadResult { Source = domainRulesPath };

        if (string.IsNullOrEmpty(domainRulesPath))
        {
            result.Success = true;
            return result;
        }

        if (!Directory.Exists(domainRulesPath))
        {
            // Domain rules directory doesn't exist - this is fine, just return empty
            result.Success = true;
            return result;
        }

        var yamlFiles = SupportedRuleExtensions
            .SelectMany(ext => Directory.GetFiles(domainRulesPath, $"*{ext}", SearchOption.TopDirectoryOnly))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        foreach (var filePath in yamlFiles)
        {
            var fileName = Path.GetFileName(filePath);
            try
            {
                var yamlContent = File.ReadAllText(filePath);
                var rulePack = _deserializer.Deserialize<RulePack>(NormalizeYamlAliases(yamlContent));

                if (rulePack?.Rules != null)
                {
                    ValidateAndAddRules(rulePack.Rules, fileName, result);
                }
            }
            catch (Exception ex)
            {
                result.Errors.Add(new RuleLoadError
                {
                    File = filePath,
                    Reason = $"Failed to parse YAML: {ex.Message}",
                    Exception = ex
                });
            }
        }

        result.Success = result.Errors.Count == 0;
        return result;
    }

    /// <summary>
    /// Loads rules from explicit YAML file paths.
    /// </summary>
    /// <param name="ruleFilePaths">Individual YAML file paths (.yaml or .yml)</param>
    public RuleLoadResult LoadRulesFromFiles(IEnumerable<string>? ruleFilePaths)
    {
        var result = new RuleLoadResult { Source = "custom-files" };

        if (ruleFilePaths == null)
        {
            result.Success = true;
            return result;
        }

        var files = ruleFilePaths
            .Where(path => !string.IsNullOrWhiteSpace(path))
            .Select(path => path.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        if (files.Length == 0)
        {
            result.Success = true;
            return result;
        }

        foreach (var filePath in files)
        {
            try
            {
                if (!File.Exists(filePath))
                {
                    result.Errors.Add(new RuleLoadError
                    {
                        File = filePath,
                        Reason = "Rule file not found"
                    });
                    continue;
                }

                var extension = Path.GetExtension(filePath);
                if (!SupportedRuleExtensions.Contains(extension, StringComparer.OrdinalIgnoreCase))
                {
                    result.Errors.Add(new RuleLoadError
                    {
                        File = filePath,
                        Reason = "Unsupported rule file extension (expected .yaml or .yml)"
                    });
                    continue;
                }

                var yamlContent = File.ReadAllText(filePath);
                var rulePack = _deserializer.Deserialize<RulePack>(NormalizeYamlAliases(yamlContent));

                if (rulePack?.Rules != null)
                {
                    ValidateAndAddRules(rulePack.Rules, filePath, result);
                }
            }
            catch (Exception ex)
            {
                result.Errors.Add(new RuleLoadError
                {
                    File = filePath,
                    Reason = $"Failed to parse YAML: {ex.Message}",
                    Exception = ex
                });
            }
        }

        result.Success = result.Errors.Count == 0;
        return result;
    }

    /// <summary>
    /// Loads all rules: builtin embedded rules plus optional domain rules from the specified path.
    /// </summary>
    /// <param name="domainRulesPath">Optional path to domain rules directory</param>
    public List<RuleDefinition> LoadAllRules(string? domainRulesPath = null)
    {
        var allRules = new List<RuleDefinition>();

        // Load builtin rules first
        var builtinResult = LoadBuiltinRules();
        allRules.AddRange(builtinResult.Rules);

        // Load domain rules if path provided
        if (!string.IsNullOrEmpty(domainRulesPath))
        {
            var domainResult = LoadDomainRules(domainRulesPath);
            allRules.AddRange(domainResult.Rules);
        }

        return allRules;
    }

    /// <summary>
    /// Loads all rules with detailed result including any errors.
    /// </summary>
    /// <param name="domainRulesPath">Optional path to domain rules directory</param>
    public (List<RuleDefinition> Rules, List<RuleLoadError> Errors) LoadAllRulesWithErrors(string? domainRulesPath = null)
    {
        var allErrors = new List<RuleLoadError>();
        var allRules = new List<RuleDefinition>();

        // Load builtin rules
        var builtinResult = LoadBuiltinRules();
        allRules.AddRange(builtinResult.Rules);
        allErrors.AddRange(builtinResult.Errors);

        // Load domain rules if path provided
        if (!string.IsNullOrEmpty(domainRulesPath))
        {
            var domainResult = LoadDomainRules(domainRulesPath);
            allRules.AddRange(domainResult.Rules);
            allErrors.AddRange(domainResult.Errors);
        }

        return (allRules, allErrors);
    }

    /// <summary>
    /// Finds the default domain rules path relative to a solution or working directory.
    /// </summary>
    /// <param name="basePath">The base path (usually solution directory)</param>
    public string? FindDomainRulesPath(string basePath)
    {
        if (string.IsNullOrEmpty(basePath))
            return null;

        var domainRulesPath = Path.Combine(basePath, DomainRulesFolderName);

        if (Directory.Exists(domainRulesPath))
            return domainRulesPath;

        return null;
    }

    /// <summary>
    /// Validates rule definitions and adds them to the result.
    /// </summary>
    private void ValidateAndAddRules(List<RuleDefinition> rules, string source, RuleLoadResult result)
    {
        foreach (var rule in rules)
        {
            if (string.IsNullOrEmpty(rule.Id))
            {
                result.Errors.Add(new RuleLoadError
                {
                    File = source,
                    Reason = "Rule missing required 'id' field"
                });
                continue;
            }

            // Normalize severity
            if (string.IsNullOrEmpty(rule.Severity))
            {
                rule.Severity = "warning";
            }

            result.Rules.Add(rule);
        }
    }

    private static string NormalizeYamlAliases(string content)
    {
        if (string.IsNullOrEmpty(content))
        {
            return content;
        }

        var normalized = content;
        foreach (var alias in YamlKeyAliases)
        {
            normalized = normalized.Replace($"{alias.Key}:", $"{alias.Value}:", StringComparison.Ordinal);
        }

        return normalized;
    }
}
