using Microsoft.Data.Sqlite;
using RoslynNavigator.Models;
using RoslynNavigator.Rules.Services;
using RoslynNavigator.Rules.Models;

namespace RoslynNavigator.Commands;

/// <summary>
/// Orchestrates rule checking: loads rules, evaluates against snapshot, applies filters.
/// </summary>
public class CheckCommand
{
    private readonly RuleLoaderService _loader;
    private readonly string _connectionString;

    public CheckCommand() : this(new RuleLoaderService())
    {
    }

    public CheckCommand(RuleLoaderService loader)
    {
        _loader = loader;
        _connectionString = string.Empty;
    }

    public CheckCommand(string dbPath) : this(new RuleLoaderService(), dbPath)
    {
    }

    public CheckCommand(RuleLoaderService loader, string dbPath)
    {
        _loader = loader;
        _connectionString = $"Data Source={dbPath}";
    }

    /// <summary>
    /// Executes the check command with optional filters.
    /// </summary>
    /// <param name="dbPath">Path to the snapshot database.</param>
    /// <param name="severityFilter">Optional severity filter (error, warning, info).</param>
    /// <param name="ruleIdFilter">Optional ruleId filter.</param>
    /// <returns>Structured result with violations.</returns>
    public async Task<CheckCommandResult> ExecuteAsync(string dbPath, string? severityFilter = null, string? ruleIdFilter = null)
    {
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        var result = new CheckCommandResult
        {
            DbPath = dbPath,
            SeverityFilter = severityFilter,
            RuleIdFilter = ruleIdFilter
        };

        try
        {
            // Validate dbPath exists
            if (!File.Exists(dbPath))
            {
                result.Success = false;
                result.ErrorMessage = $"Snapshot database not found: {dbPath}";
                stopwatch.Stop();
                result.ElapsedMs = stopwatch.ElapsedMilliseconds;
                return result;
            }

            // Load all rules
            var rules = _loader.LoadAllRules();
            
            if (rules.Count == 0)
            {
                result.Success = true;
                result.TotalRulesEvaluated = 0;
                result.TotalViolations = 0;
                result.FilteredViolations = 0;
                stopwatch.Stop();
                result.ElapsedMs = stopwatch.ElapsedMilliseconds;
                return result;
            }

            // Evaluate rules against snapshot
            var evaluator = new RuleEvaluatorService($"Data Source={dbPath}");
            var evaluationResult = evaluator.EvaluateAll(rules);

            if (!evaluationResult.Success)
            {
                result.Success = false;
                result.ErrorMessage = evaluationResult.ErrorMessage;
                stopwatch.Stop();
                result.ElapsedMs = stopwatch.ElapsedMilliseconds;
                return result;
            }

            result.TotalRulesEvaluated = evaluationResult.RulesEvaluated;
            result.TotalViolations = evaluationResult.Violations.Count;

            // Apply filters
            var filteredViolations = evaluationResult.Violations.AsEnumerable();

            if (!string.IsNullOrEmpty(severityFilter))
            {
                var parsedSeverity = RuleSeverityExtensions.ParseSeverity(severityFilter);
                filteredViolations = filteredViolations.Where(v => v.Severity == parsedSeverity);
            }

            if (!string.IsNullOrEmpty(ruleIdFilter))
            {
                filteredViolations = filteredViolations.Where(v => v.RuleId.Contains(ruleIdFilter, StringComparison.OrdinalIgnoreCase));
            }

            result.Violations = filteredViolations.ToList();
            result.FilteredViolations = result.Violations.Count;
            result.Success = true;
        }
        catch (Exception ex)
        {
            result.Success = false;
            result.ErrorMessage = ex.Message;
        }

        stopwatch.Stop();
        result.ElapsedMs = stopwatch.ElapsedMilliseconds;
        return result;
    }

    /// <summary>
    /// Static method to execute check with explicit services (for DI in CLI).
    /// </summary>
    public static async Task<CheckCommandResult> ExecuteAsync(
        string dbPath,
        string? severityFilter,
        string? ruleIdFilter,
        RuleLoaderService loader)
    {
        var command = new CheckCommand(loader, dbPath);
        return await command.ExecuteAsync(dbPath, severityFilter, ruleIdFilter);
    }
}
