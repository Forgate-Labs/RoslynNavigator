using System.Text;
using RoslynNavigator.Models;

namespace RoslynNavigator.Services;

/// <summary>
/// Compiles rule predicates into SQL queries against the snapshot database.
/// </summary>
public class RuleSqlCompiler
{
    private int _paramCounter = 0;

    /// <summary>
    /// Compiles a rule predicate into SQL and returns the SQL string and parameters.
    /// </summary>
    public (string Sql, Dictionary<string, object?> Parameters) Compile(RulePredicate predicate)
    {
        _paramCounter = 0;
        var parameters = new Dictionary<string, object?>();
        
        var (whereClause, whereParams) = BuildWhereClause(predicate);
        foreach (var kvp in whereParams)
        {
            parameters[kvp.Key] = kvp.Value;
        }

        string sql;
        if (string.IsNullOrEmpty(whereClause))
        {
            // No conditions - return all classes with methods
            sql = @"SELECT cls.namespace, cls.name, m.name as method_name, m.start_line as line_number 
                    FROM classes cls 
                    JOIN methods m ON m.class_id = cls.id";
        }
        else
        {
            // Check if this query needs the calls table
            // If it only has class-level conditions (like fromNamespace alone), 
            // we can query without the calls table
            if (whereClause.Contains("c."))
            {
                // Query with calls table
                sql = $@"SELECT DISTINCT cls.namespace, cls.name, m.name as method_name, m.start_line as line_number 
                        FROM calls c 
                        JOIN methods m ON c.caller_method_id = m.id 
                        JOIN classes cls ON m.class_id = cls.id 
                        WHERE {whereClause}";
            }
            else
            {
                // Query without calls table - just class/method filtering
                sql = $@"SELECT DISTINCT cls.namespace, cls.name, m.name as method_name, m.start_line as line_number 
                        FROM classes cls 
                        JOIN methods m ON m.class_id = cls.id 
                        WHERE {whereClause}";
            }
        }

        return (sql, parameters);
    }

    private (string WhereClause, Dictionary<string, object?> Parameters) BuildWhereClause(RulePredicate predicate)
    {
        var parameters = new Dictionary<string, object?>();
        var conditions = new List<string>();

        // Handle 'calls' predicate - look for calls matching pattern
        if (!string.IsNullOrEmpty(predicate.Calls))
        {
            var paramName = $"@p{_paramCounter++}";
            
            if (predicate.Calls.Contains("*"))
            {
                // Convert wildcard to SQL LIKE pattern
                // For "Controller.*", we want to match:
                // - "Controller" (just the class)
                // - "Controller.Save" (class + method)  
                // - "MyApp.Controller" (namespace.class)
                // We convert to "Controller%" which matches all of these
                var pattern = predicate.Calls.Replace(".*", "%").Replace("*", "%");
                
                // Match class name or fully qualified name
                conditions.Add($"(c.target_class LIKE {paramName} OR (c.target_namespace || '.' || c.target_class) LIKE {paramName})");
                parameters[paramName] = pattern;
            }
            else
            {
                // Exact match - match either just class or fully qualified
                conditions.Add($"(c.target_class = {paramName} OR (c.target_namespace || '.' || c.target_class) = {paramName})");
                parameters[paramName] = predicate.Calls;
            }
        }

        // Handle 'fromNamespace' predicate - filter caller class namespace
        if (!string.IsNullOrEmpty(predicate.FromNamespace))
        {
            var paramName = $"@p{_paramCounter++}";
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

        // Handle 'returns_null' predicate - filter on method returns_null flag
        if (predicate.ReturnsNull.HasValue)
        {
            var paramName = $"@p{_paramCounter++}";
            conditions.Add($"m.returns_null = {paramName}");
            parameters[paramName] = predicate.ReturnsNull.Value ? 1 : 0;
        }

        // Handle 'cognitive_complexity' predicate - filter on complexity threshold
        if (predicate.CognitiveComplexity.HasValue)
        {
            var paramName = $"@p{_paramCounter++}";
            conditions.Add($"m.cognitive_complexity >= {paramName}");
            parameters[paramName] = predicate.CognitiveComplexity.Value;
        }

        // Handle 'has_try_catch' predicate
        if (predicate.HasTryCatch.HasValue)
        {
            var paramName = $"@p{_paramCounter++}";
            conditions.Add($"m.has_try_catch = {paramName}");
            parameters[paramName] = predicate.HasTryCatch.Value ? 1 : 0;
        }

        // Handle 'accesses_db' predicate - filter on method accesses_db flag
        if (predicate.AccessesDb.HasValue)
        {
            var paramName = $"@p{_paramCounter++}";
            conditions.Add($"m.accesses_db = {paramName}");
            parameters[paramName] = predicate.AccessesDb.Value ? 1 : 0;
        }

        // Handle 'calls_external' predicate - filter on method calls_external flag
        if (predicate.CallsExternal.HasValue)
        {
            var paramName = $"@p{_paramCounter++}";
            conditions.Add($"m.calls_external = {paramName}");
            parameters[paramName] = predicate.CallsExternal.Value ? 1 : 0;
        }

        // Handle 'filters_by_tenant' predicate - filter on method filters_by_tenant flag
        if (predicate.FiltersByTenant.HasValue)
        {
            var paramName = $"@p{_paramCounter++}";
            conditions.Add($"m.filters_by_tenant = {paramName}");
            parameters[paramName] = predicate.FiltersByTenant.Value ? 1 : 0;
        }

        // Handle 'not' predicate - wraps nested condition with NOT EXISTS
        if (predicate.Not != null)
        {
            _paramCounter = 0; // Reset counter for nested
            var (nestedWhere, nestedParams) = BuildWhereClause(predicate.Not);
            
            // Build NOT EXISTS subquery
            var notExistsParamNames = new Dictionary<string, string>();
            foreach (var kvp in nestedParams)
            {
                var newName = $"@not{_paramCounter++}";
                notExistsParamNames[kvp.Key] = newName;
                parameters[newName] = kvp.Value;
            }

            var renamedWhere = nestedWhere;
            foreach (var mapping in notExistsParamNames)
            {
                renamedWhere = renamedWhere.Replace(mapping.Key, mapping.Value);
            }

            // Only add NOT EXISTS if there's a valid nested condition
            if (!string.IsNullOrEmpty(renamedWhere))
            {
                var notExistsClause = $@"
                    NOT EXISTS (
                        SELECT 1 FROM calls c2 
                        JOIN methods m2 ON c2.caller_method_id = m2.id 
                        JOIN classes cls2 ON m2.class_id = cls2.id 
                        WHERE {renamedWhere}
                    )";
                
                conditions.Add(notExistsClause);
            }
        }

        var whereClause = conditions.Count > 0 ? string.Join(" AND ", conditions) : "";
        return (whereClause, parameters);
    }
}
