using Microsoft.Data.Sqlite;
using RoslynNavigator.Rules.Models;

namespace RoslynNavigator.Rules.Services;

/// <summary>
/// Evaluates rules against a snapshot database and returns violations.
/// </summary>
public class RuleEvaluatorService
{
    private readonly string _connectionString;
    private readonly RuleSqlCompiler _compiler;
    private readonly SqlReadOnlyGuard _readOnlyGuard;

    public RuleEvaluatorService(string connectionString)
    {
        _connectionString = connectionString;
        _compiler = new RuleSqlCompiler();
        _readOnlyGuard = new SqlReadOnlyGuard();
    }

    /// <summary>
    /// Evaluates a single rule against the snapshot database.
    /// </summary>
    public RuleEvaluationResult Evaluate(RuleDefinition rule)
    {
        var result = new RuleEvaluationResult
        {
            RulesEvaluated = 1
        };

        if (rule.Predicate == null)
        {
            result.Success = true;
            return result;
        }

        try
        {
            // Compile the predicate to SQL
            var (sql, parameters) = _compiler.Compile(rule.Predicate);

            // Validate SQL is read-only using shared guard
            var validation = _readOnlyGuard.Validate(sql);
            if (!validation.IsValid)
            {
                result.Success = false;
                result.ErrorMessage = validation.Reason;
                return result;
            }

            // Execute the query
            using var connection = new SqliteConnection(_connectionString);
            connection.Open();

            using var command = connection.CreateCommand();
            command.CommandText = sql;

            // Add parameters
            foreach (var kvp in parameters)
            {
                command.Parameters.AddWithValue(kvp.Key, kvp.Value ?? DBNull.Value);
            }

            using var reader = command.ExecuteReader();

            while (reader.Read())
            {
                var violation = new RuleViolation
                {
                    RuleId = rule.Id,
                    Severity = RuleSeverityExtensions.ParseSeverity(rule.Severity),
                    Message = rule.Message,
                    Namespace = reader.GetString(0),
                    ClassName = reader.GetString(1),
                    MethodName = reader.IsDBNull(2) ? null : reader.GetString(2),
                    LineNumber = reader.GetInt32(3)
                };

                result.Violations.Add(violation);
            }

            result.Success = true;
        }
        catch (Exception ex)
        {
            result.Success = false;
            result.ErrorMessage = $"Evaluation failed: {ex.Message}";
        }

        return result;
    }

    /// <summary>
    /// Evaluates multiple rules against the snapshot database.
    /// </summary>
    public RuleEvaluationResult EvaluateAll(IEnumerable<RuleDefinition> rules)
    {
        var result = new RuleEvaluationResult();

        foreach (var rule in rules)
        {
            var ruleResult = Evaluate(rule);
            
            if (!ruleResult.Success)
            {
                result.Success = false;
                result.ErrorMessage = ruleResult.ErrorMessage;
                return result;
            }

            result.Violations.AddRange(ruleResult.Violations);
            result.RulesEvaluated++;
        }

        result.Success = true;
        return result;
    }
}
