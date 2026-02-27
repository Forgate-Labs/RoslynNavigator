namespace RoslynNavigator.Models;

/// <summary>
/// Result of the check command execution.
/// </summary>
public class CheckCommandResult
{
    /// <summary>
    /// Whether the check completed successfully.
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Path to the snapshot database that was checked.
    /// </summary>
    public string DbPath { get; set; } = string.Empty;

    /// <summary>
    /// The total number of rules that were evaluated.
    /// </summary>
    public int TotalRulesEvaluated { get; set; }

    /// <summary>
    /// The total number of violations found before filtering.
    /// </summary>
    public int TotalViolations { get; set; }

    /// <summary>
    /// The number of violations returned after applying filters.
    /// </summary>
    public int FilteredViolations { get; set; }

    /// <summary>
    /// The severity filter that was applied (if any).
    /// </summary>
    public string? SeverityFilter { get; set; }

    /// <summary>
    /// The ruleId filter that was applied (if any).
    /// </summary>
    public string? RuleIdFilter { get; set; }

    /// <summary>
    /// List of violations found (after filtering).
    /// </summary>
    public List<RuleViolation> Violations { get; set; } = new();

    /// <summary>
    /// Error message if check failed.
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Time taken to execute the check in milliseconds.
    /// </summary>
    public long ElapsedMs { get; set; }
}
