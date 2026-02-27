using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace RoslynNavigator.Snapshot.Services;

/// <summary>
/// Analyzes C# syntax to extract analysis signals for snapshot data.
/// </summary>
public class SnapshotSignalAnalyzer
{
    // Database-related method patterns
    private static readonly string[] DatabasePatterns = new[]
    {
        "Execute", "Query", "ExecuteAsync", "QueryAsync",
        "SaveChanges", "SaveChangesAsync", "Add", "Update", "Delete",
        "Insert", "Remove", "GetById", "Find", "FirstOrDefault",
        "ToList", "ToListAsync", "SingleOrDefault", "FirstAsync"
    };

    // External API call patterns
    private static readonly string[] ExternalApiPatterns = new[]
    {
        "HttpClient", "HttpRequest", "RestClient", "WebClient",
        "Fetch", "axios", "Request", "Response"
    };

    /// <summary>
    /// Analyzes a method declaration and extracts all required signals.
    /// </summary>
    public MethodSignals AnalyzeMethod(MethodDeclarationSyntax method, SemanticModel semanticModel)
    {
        var signals = new MethodSignals();

        signals.ParameterCount = method.ParameterList.Parameters.Count;

        // Returns null detection
        signals.ReturnsNull = AnalyzeReturnsNull(method, semanticModel);

        // Cognitive complexity
        signals.CognitiveComplexity = AnalyzeCognitiveComplexity(method);

        // Try/catch detection
        signals.HasTryCatch = AnalyzeTryCatch(method);

        // External calls detection
        signals.CallsExternal = AnalyzeExternalCalls(method, semanticModel);

        // Database access detection
        signals.AccessesDb = AnalyzeDatabaseAccess(method, semanticModel);

        // Tenant filtering detection
        signals.FiltersByTenant = AnalyzeTenantFiltering(method, semanticModel);

        signals.UsesInsecureRandom = AnalyzeInsecureRandomUsage(method, semanticModel);
        signals.UsesWeakCrypto = AnalyzeWeakCryptoUsage(method, semanticModel);
        signals.CatchesGeneralException = AnalyzeCatchGeneralException(method, semanticModel);
        signals.ThrowsGeneralException = AnalyzeThrowGeneralException(method, semanticModel);
        signals.HasSqlStringConcatenation = AnalyzeSqlStringConcatenation(method, semanticModel);
        signals.HasHardcodedSecret = AnalyzeHardcodedSecret(method);

        return signals;
    }

    /// <summary>
    /// Analyzes a class declaration and extracts signals.
    /// </summary>
    public ClassSignals AnalyzeClass(ClassDeclarationSyntax classDecl, SemanticModel semanticModel)
    {
        var signals = new ClassSignals();

        // Check if class returns null (e.g., factory methods)
        signals.ReturnsNull = AnalyzeClassReturnsNull(classDecl);

        // Analyze cognitive complexity from all methods
        signals.CognitiveComplexity = AnalyzeClassComplexity(classDecl);

        // Check for try/catch in methods
        signals.HasTryCatch = classDecl.DescendantNodes()
            .OfType<MethodDeclarationSyntax>()
            .Any(m => m.Body != null && m.Body.DescendantNodes().Any(n => n is TryStatementSyntax));

        // Check for external calls
        signals.CallsExternal = classDecl.DescendantNodes()
            .OfType<MethodDeclarationSyntax>()
            .Any(m => HasExternalCallInvocation(m, semanticModel));

        // Check for database access
        signals.AccessesDb = classDecl.DescendantNodes()
            .OfType<MethodDeclarationSyntax>()
            .Any(m => HasDatabaseAccessInvocation(m, semanticModel));

        // Check for tenant filtering
        signals.FiltersByTenant = AnalyzeClassTenantFiltering(classDecl, semanticModel);

        return signals;
    }

    private bool AnalyzeReturnsNull(MethodDeclarationSyntax method, SemanticModel semanticModel)
    {
        // Check return type - if nullable reference type
        var returnType = method.ReturnType;
        if (returnType != null)
        {
            var typeInfo = semanticModel.GetTypeInfo(returnType);
            if (typeInfo.Type != null)
            {
                // Check if nullable reference type
                if (typeInfo.Type.IsReferenceType && 
                    returnType is NullableTypeSyntax)
                {
                    return true;
                }

                // Common null-return patterns
                var typeName = typeInfo.Type.Name.ToLower();
                if (typeName.Contains("task") || 
                    typeName.Contains("result") ||
                    typeName.Contains("optional"))
                {
                    // Check for nullable return
                    if (returnType is NullableTypeSyntax)
                        return true;
                }
            }
        }

        // Check for explicit null returns in body
        if (method.Body != null)
        {
            var nullReturns = method.Body.DescendantNodes()
                .OfType<ReturnStatementSyntax>()
                .Where(r => r.Expression?.Kind() == SyntaxKind.NullLiteralExpression)
                .Any();
            
            if (nullReturns) return true;
        }

        return false;
    }

    private int AnalyzeCognitiveComplexity(MethodDeclarationSyntax method)
    {
        if (method.Body == null) return 0;

        var complexity = 0;

        // Base complexity = 1
        complexity++;

        // Increment for each control flow complexity point
        foreach (var node in method.Body.DescendantNodes())
        {
            switch (node.Kind())
            {
                // Increments
                case SyntaxKind.IfStatement:
                case SyntaxKind.ForStatement:
                case SyntaxKind.ForEachStatement:
                case SyntaxKind.WhileStatement:
                case SyntaxKind.DoStatement:
                case SyntaxKind.CaseSwitchLabel:
                case SyntaxKind.CatchClause:
                case SyntaxKind.ConditionalExpression:
                    complexity++;
                    break;

                // Increments by nesting level
                case SyntaxKind.LogicalAndExpression:
                case SyntaxKind.LogicalOrExpression:
                    complexity++;
                    break;
            }
        }

        return complexity;
    }

    private bool AnalyzeTryCatch(MethodDeclarationSyntax method)
    {
        if (method.Body == null) return false;

        return method.Body.DescendantNodes()
            .Any(n => n is TryStatementSyntax);
    }

    private bool AnalyzeExternalCalls(MethodDeclarationSyntax method, SemanticModel semanticModel)
    {
        return HasExternalCallInvocation(method, semanticModel);
    }

    private bool HasExternalCallInvocation(MethodDeclarationSyntax method, SemanticModel semanticModel)
    {
        if (method.Body == null) return false;

        foreach (var invocation in method.Body.DescendantNodes().OfType<InvocationExpressionSyntax>())
        {
            var symbol = semanticModel.GetSymbolInfo(invocation).Symbol;
            if (symbol != null)
            {
                var typeName = symbol.ContainingType?.Name ?? "";
                if (ExternalApiPatterns.Any(p => typeName.Contains(p, StringComparison.OrdinalIgnoreCase)))
                {
                    return true;
                }
            }
        }

        return false;
    }

    private bool AnalyzeDatabaseAccess(MethodDeclarationSyntax method, SemanticModel semanticModel)
    {
        return HasDatabaseAccessInvocation(method, semanticModel);
    }

    private bool HasDatabaseAccessInvocation(MethodDeclarationSyntax method, SemanticModel semanticModel)
    {
        if (method.Body == null) return false;

        foreach (var invocation in method.Body.DescendantNodes().OfType<InvocationExpressionSyntax>())
        {
            var symbol = semanticModel.GetSymbolInfo(invocation).Symbol;
            if (symbol != null)
            {
                var methodName = symbol.Name;
                var typeName = symbol.ContainingType?.Name ?? "";
                var namespaceName = symbol.ContainingType?.ContainingNamespace?.ToDisplayString() ?? "";

                var dbTypeNames = new[] { "DbContext", "DbSet", "SqlConnection", "SqlCommand", "NpgsqlConnection", "MySqlConnection", "OracleConnection" };
                var dbNamespaces = new[]
                {
                    "Microsoft.EntityFrameworkCore",
                    "System.Data",
                    "Microsoft.Data.SqlClient",
                    "Npgsql",
                    "Dapper"
                };

                if (dbTypeNames.Any(t => typeName.Contains(t, StringComparison.OrdinalIgnoreCase)) ||
                    dbNamespaces.Any(n => namespaceName.StartsWith(n, StringComparison.OrdinalIgnoreCase)))
                {
                    return true;
                }

                if (typeName.Contains("Repository", StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }

                if (DatabasePatterns.Any(p => methodName.Contains(p, StringComparison.OrdinalIgnoreCase)) &&
                    typeName.Contains("Repository", StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }
        }

        return false;
    }

    private bool AnalyzeInsecureRandomUsage(MethodDeclarationSyntax method, SemanticModel semanticModel)
    {
        if (method.Body == null) return false;

        foreach (var invocation in method.Body.DescendantNodes().OfType<InvocationExpressionSyntax>())
        {
            var symbol = semanticModel.GetSymbolInfo(invocation).Symbol;
            var type = symbol?.ContainingType;
            if (type == null) continue;

            if (type.ToDisplayString() == "System.Random")
            {
                return true;
            }
        }

        return false;
    }

    private bool AnalyzeWeakCryptoUsage(MethodDeclarationSyntax method, SemanticModel semanticModel)
    {
        if (method.Body == null) return false;

        var weakTypes = new[]
        {
            "System.Security.Cryptography.MD5",
            "System.Security.Cryptography.SHA1",
            "System.Security.Cryptography.DES",
            "System.Security.Cryptography.RC2",
            "System.Security.Cryptography.TripleDES"
        };

        foreach (var invocation in method.Body.DescendantNodes().OfType<InvocationExpressionSyntax>())
        {
            var symbol = semanticModel.GetSymbolInfo(invocation).Symbol;
            var owner = symbol?.ContainingType?.ToDisplayString() ?? string.Empty;
            if (weakTypes.Any(owner.Equals))
            {
                return true;
            }
        }

        foreach (var creation in method.Body.DescendantNodes().OfType<ObjectCreationExpressionSyntax>())
        {
            var type = semanticModel.GetTypeInfo(creation).Type?.ToDisplayString() ?? string.Empty;
            if (weakTypes.Any(type.Equals))
            {
                return true;
            }
        }

        return false;
    }

    private bool AnalyzeCatchGeneralException(MethodDeclarationSyntax method, SemanticModel semanticModel)
    {
        if (method.Body == null) return false;

        foreach (var catchClause in method.Body.DescendantNodes().OfType<CatchClauseSyntax>())
        {
            if (catchClause.Declaration == null)
            {
                return true;
            }

            var type = semanticModel.GetTypeInfo(catchClause.Declaration.Type).Type?.ToDisplayString() ?? string.Empty;
            if (type == "System.Exception")
            {
                return true;
            }
        }

        return false;
    }

    private bool AnalyzeThrowGeneralException(MethodDeclarationSyntax method, SemanticModel semanticModel)
    {
        if (method.Body == null) return false;

        foreach (var throwStmt in method.Body.DescendantNodes().OfType<ThrowStatementSyntax>())
        {
            if (throwStmt.Expression is not ObjectCreationExpressionSyntax objCreation)
            {
                continue;
            }

            var type = semanticModel.GetTypeInfo(objCreation).Type?.ToDisplayString() ?? string.Empty;
            if (type is "System.Exception" or "System.ApplicationException")
            {
                return true;
            }
        }

        return false;
    }

    private bool AnalyzeSqlStringConcatenation(MethodDeclarationSyntax method, SemanticModel semanticModel)
    {
        if (method.Body == null) return false;

        foreach (var invocation in method.Body.DescendantNodes().OfType<InvocationExpressionSyntax>())
        {
            var symbol = semanticModel.GetSymbolInfo(invocation).Symbol;
            if (symbol == null)
            {
                continue;
            }

            var methodName = symbol.Name;
            var owner = symbol.ContainingType?.ToDisplayString() ?? string.Empty;
            var isSqlExecution = methodName.Contains("Query", StringComparison.OrdinalIgnoreCase)
                || methodName.Contains("Execute", StringComparison.OrdinalIgnoreCase)
                || methodName.Contains("FromSql", StringComparison.OrdinalIgnoreCase)
                || owner.Contains("SqlCommand", StringComparison.OrdinalIgnoreCase);

            if (!isSqlExecution)
            {
                continue;
            }

            foreach (var argument in invocation.ArgumentList.Arguments)
            {
                if (argument.Expression is BinaryExpressionSyntax binary && binary.IsKind(SyntaxKind.AddExpression))
                {
                    return true;
                }

                if (argument.Expression is InterpolatedStringExpressionSyntax)
                {
                    return true;
                }
            }
        }

        return false;
    }

    private bool AnalyzeHardcodedSecret(MethodDeclarationSyntax method)
    {
        if (method.Body == null) return false;

        var secretPattern = new[] { "password", "passwd", "secret", "token", "apikey", "api_key", "privatekey", "private_key" };

        foreach (var literal in method.Body.DescendantNodes().OfType<LiteralExpressionSyntax>())
        {
            if (!literal.IsKind(SyntaxKind.StringLiteralExpression))
            {
                continue;
            }

            var text = literal.Token.ValueText;
            if (text.Length < 6)
            {
                continue;
            }

            if (secretPattern.Any(pattern => text.Contains(pattern, StringComparison.OrdinalIgnoreCase)))
            {
                return true;
            }
        }

        return false;
    }

    private bool AnalyzeTenantFiltering(MethodDeclarationSyntax method, SemanticModel semanticModel)
    {
        if (method.Body == null) return false;

        // Look for tenant-related patterns in the method
        var methodText = method.ToString();
        
        // Check for tenant in parameters
        foreach (var param in method.ParameterList.Parameters)
        {
            if (param.Identifier.Text.Contains("tenant", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        // Check for tenant in method body
        return method.Body.DescendantTokens()
            .Any(t => t.Text.Contains("tenant", StringComparison.OrdinalIgnoreCase));
    }

    private bool AnalyzeClassReturnsNull(ClassDeclarationSyntax classDecl)
    {
        // Look for factory patterns or nullable return methods
        return classDecl.DescendantNodes()
            .OfType<MethodDeclarationSyntax>()
            .Any(m => m.ReturnType is NullableTypeSyntax);
    }

    private int AnalyzeClassComplexity(ClassDeclarationSyntax classDecl)
    {
        return classDecl.DescendantNodes()
            .OfType<MethodDeclarationSyntax>()
            .Sum(m => AnalyzeCognitiveComplexity(m));
    }

    private bool AnalyzeClassTenantFiltering(ClassDeclarationSyntax classDecl, SemanticModel semanticModel)
    {
        // Check entire class for tenant references
        var classText = classDecl.ToString();
        
        // Check for tenant in class members
        return classDecl.DescendantTokens()
            .Any(t => t.Text.Contains("tenant", StringComparison.OrdinalIgnoreCase));
    }
}

/// <summary>
/// Signals extracted from a method.
/// </summary>
public class MethodSignals
{
    public bool ReturnsNull { get; set; }
    public int CognitiveComplexity { get; set; }
    public bool HasTryCatch { get; set; }
    public bool CallsExternal { get; set; }
    public bool AccessesDb { get; set; }
    public bool FiltersByTenant { get; set; }
    public int ParameterCount { get; set; }
    public bool UsesInsecureRandom { get; set; }
    public bool UsesWeakCrypto { get; set; }
    public bool CatchesGeneralException { get; set; }
    public bool ThrowsGeneralException { get; set; }
    public bool HasSqlStringConcatenation { get; set; }
    public bool HasHardcodedSecret { get; set; }
}

/// <summary>
/// Signals extracted from a class.
/// </summary>
public class ClassSignals
{
    public bool ReturnsNull { get; set; }
    public int CognitiveComplexity { get; set; }
    public bool HasTryCatch { get; set; }
    public bool CallsExternal { get; set; }
    public bool AccessesDb { get; set; }
    public bool FiltersByTenant { get; set; }
}
