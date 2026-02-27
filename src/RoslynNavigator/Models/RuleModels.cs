using System.Text.Json.Serialization;

namespace RoslynNavigator.Models;

/// <summary>
/// Represents a loaded rule pack containing multiple rule definitions.
/// </summary>
public class RulePack
{
    [JsonPropertyName("rules")]
    public List<RuleDefinition> Rules { get; set; } = new();
}

/// <summary>
/// Represents a single rule definition loaded from YAML.
/// </summary>
public class RuleDefinition
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;

    [JsonPropertyName("severity")]
    public string Severity { get; set; } = "warning";

    [JsonPropertyName("message")]
    public string Message { get; set; } = string.Empty;

    [JsonPropertyName("predicate")]
    public RulePredicate? Predicate { get; set; }
}

/// <summary>
/// Represents the predicate conditions for a rule.
/// Maps to the 'predicate' block in YAML.
/// </summary>
public class RulePredicate
{
    /// <summary>
    /// Method calls pattern (supports wildcard like "Controller.*")
    /// </summary>
    [JsonPropertyName("calls")]
    public string? Calls { get; set; }

    /// <summary>
    /// Filter by source namespace pattern
    /// </summary>
    [JsonPropertyName("fromNamespace")]
    public string? FromNamespace { get; set; }

    /// <summary>
    /// Negation block - rule matches when inner conditions are NOT met
    /// </summary>
    [JsonPropertyName("not")]
    public RulePredicate? Not { get; set; }

    /// <summary>
    /// Match when method returns null
    /// </summary>
    [JsonPropertyName("returns_null")]
    public bool? ReturnsNull { get; set; }

    /// <summary>
    /// Match when cognitive complexity exceeds threshold
    /// </summary>
    [JsonPropertyName("cognitive_complexity")]
    public int? CognitiveComplexity { get; set; }

    /// <summary>
    /// Match when method has try-catch
    /// </summary>
    [JsonPropertyName("has_try_catch")]
    public bool? HasTryCatch { get; set; }

    /// <summary>
    /// Match when method calls external services
    /// </summary>
    [JsonPropertyName("calls_external")]
    public bool? CallsExternal { get; set; }

    /// <summary>
    /// Match when method accesses database
    /// </summary>
    [JsonPropertyName("accesses_db")]
    public bool? AccessesDb { get; set; }

    /// <summary>
    /// Match when method filters by tenant
    /// </summary>
    [JsonPropertyName("filters_by_tenant")]
    public bool? FiltersByTenant { get; set; }

    /// <summary>
    /// Match when method should validate input
    /// </summary>
    [JsonPropertyName("method_should_validate_input")]
    public bool? MethodShouldValidateInput { get; set; }
}

/// <summary>
/// Result of loading rules, including any errors encountered.
/// </summary>
public class RuleLoadResult
{
    public bool Success { get; set; }
    public List<RuleDefinition> Rules { get; set; } = new();
    public List<RuleLoadError> Errors { get; set; } = new();
    public string? Source { get; set; }
}

/// <summary>
/// Represents an error encountered while loading rules.
/// </summary>
public class RuleLoadError
{
    public string File { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
    public int? Line { get; set; }
    public Exception? Exception { get; set; }
}

/// <summary>
/// Rule severity levels.
/// </summary>
public enum RuleSeverity
{
    Info,
    Warning,
    Error
}

public static class RuleSeverityExtensions
{
    public static RuleSeverity ParseSeverity(string value)
    {
        return value.ToLowerInvariant() switch
        {
            "info" => RuleSeverity.Info,
            "warning" => RuleSeverity.Warning,
            "error" => RuleSeverity.Error,
            _ => RuleSeverity.Warning
        };
    }
}
