using System.Text;
using RoslynNavigator.Models;

namespace RoslynNavigator.Services;

/// <summary>
/// Compiles rule predicates into SQL queries against the snapshot database.
/// </summary>
public class RuleSqlCompiler
{
    /// <summary>
    /// Compiles a rule predicate into SQL and returns the SQL string and parameters.
    /// </summary>
    public (string Sql, Dictionary<string, object?> Parameters) Compile(RulePredicate predicate)
    {
        var parameters = new Dictionary<string, object?>();
        var sql = new StringBuilder();

        // Build WHERE clause based on predicate
        var conditions = new List<string>();

        // Handle 'calls' predicate
        if (!string.IsNullOrEmpty(predicate.Calls))
        {
            var paramName = $"@{conditions.Count}";
            var pattern = predicate.Calls.Replace("*", "%");
            
            if (predicate.Calls.Contains("*"))
            {
                conditions.Add($"c.target_class LIKE {paramName}");
                parameters[paramName] = pattern;
            }
            else
            {
                conditions.Add($"c.target_class = {paramName}");
                parameters[paramName] = pattern;
            }
        }

        // Handle 'fromNamespace' predicate
        if (!string.IsNullOrEmpty(predicate.FromNamespace))
        {
            var paramName = $"@{conditions.Count}";
            var pattern = predicate.FromNamespace.Replace("*", "%");
            
            if (predicate.FromNamespace.Contains("*"))
            {
                conditions.Add($"cls.namespace LIKE {paramName}");
                parameters[paramName] = pattern;
            }
            else
            {
                conditions.Add($"cls.namespace = {paramName}");
                parameters[paramName] = pattern;
            }
        }

        // Handle 'returns_null' predicate
        if (predicate.ReturnsNull.HasValue)
        {
            var paramName = $"@{conditions.Count}";
            conditions.Add($"m.returns_null = {paramName}");
            parameters[paramName] = predicate.ReturnsNull.Value ? 1 : 0;
        }

        // Handle 'cognitive_complexity' predicate
        if (predicate.CognitiveComplexity.HasValue)
        {
            var paramName = $"@{conditions.Count}";
            conditions.Add($"m.cognitive_complexity >= {paramName}");
            parameters[paramName] = predicate.CognitiveComplexity.Value;
        }

        // Handle 'has_try_catch' predicate
        if (predicate.HasTryCatch.HasValue)
        {
            var paramName = $"@{conditions.Count}";
            conditions.Add($"m.has_try_catch = {paramName}");
            parameters[paramName] = predicate.HasTryCatch.Value ? 1 : 0;
        }

        // Handle 'not' predicate - wraps nested condition with NOT EXISTS
        if (predicate.Not != null)
        {
            var (nestedSql, nestedParams) = Compile(predicate.Not);
            foreach (var kvp in nestedParams)
            {
                parameters[$"not_{kvp.Key}"] = kvp.Value;
            }
            
            // Wrap with NOT EXISTS subquery
            var notExistsSql = $@"
                NOT EXISTS (
                    SELECT 1 FROM calls c
                    JOIN methods m ON c.caller_method_id = m.id
                    JOIN classes cls ON m.class_id = cls.id
                    WHERE {nestedSql.Replace("@", "@not_")}
                )";
            
            // If there are other conditions, we need to restructure
            // For now, return NOT EXISTS with the nested condition
            return (notExistsSql, parameters);
        }

        // Build the final SQL
        if (conditions.Count > 0)
        {
            sql.Append("SELECT DISTINCT cls.namespace, cls.name, m.name, m.start_line ");
            sql.Append("FROM calls c ");
            sql.Append("JOIN methods m ON c.caller_method_id = m.id ");
            sql.Append("JOIN classes cls ON m.class_id = cls.id ");
            sql.Append("WHERE ");
            sql.Append(string.Join(" AND ", conditions));
        }
        else
        {
            // No conditions - return all classes
            sql.Append("SELECT cls.namespace, cls.name, NULL as method_name, 0 as line_number ");
            sql.Append("FROM classes cls ");
        }

        return (sql.ToString(), parameters);
    }
}
