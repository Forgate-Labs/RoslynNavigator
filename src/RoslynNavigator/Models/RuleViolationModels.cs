namespace RoslynNavigator.Models;

/// <summary>
/// Represents a single rule violation found during evaluation.
/// </summary>
public class RuleViolation
{
    /// <summary>
    /// The rule ID that triggered this violation.
    /// </summary>
    public string RuleId { get; set; } = string.Empty;

    /// <summary>
    /// The severity level of the violation.
    /// </summary>
    public RuleSeverity Severity { get; set; }

    /// <summary>
    /// Human-readable message describing the violation.
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// The namespace of the class where violation was found.
    /// </summary>
    public string Namespace { get; set; } = string.Empty;

    /// <summary>
    /// The name of the class where violation was found.
    /// </summary>
    public string ClassName { get; set; } = string.Empty;

    /// <summary>
    /// The method name where violation was found (if applicable).
    /// </summary>
    public string? MethodName { get; set; }

    /// <summary>
    /// The file path where the violation was found.
    /// </summary>
    public string FilePath { get; set; } = string.Empty;

    /// <summary>
    /// The line number where the violation was found.
    /// </summary>
    public int LineNumber { get; set; }
}

/// <summary>
/// Result of rule evaluation against a snapshot.
/// </summary>
public class RuleEvaluationResult
{
    /// <summary>
    /// Whether the evaluation completed successfully.
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// List of violations found during evaluation.
    /// </summary>
    public List<RuleViolation> Violations { get; set; } = new();

    /// <summary>
    /// Error message if evaluation failed.
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// The number of rules that were evaluated.
    /// </summary>
    public int RulesEvaluated { get; set; }
}
