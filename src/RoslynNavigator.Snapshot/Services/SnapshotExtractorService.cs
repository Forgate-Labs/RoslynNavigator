using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Workspaces;
using Microsoft.Data.Sqlite;

namespace RoslynNavigator.Snapshot.Services;

/// <summary>
/// Extracts code structure and signals from a solution and persists to SQLite.
/// </summary>
public class SnapshotExtractorService
{
    private readonly SnapshotSchemaService _schemaService;
    private readonly SnapshotSignalAnalyzer _signalAnalyzer;
    private readonly SnapshotPathService _pathService;

    public SnapshotExtractorService(
        SnapshotSchemaService schemaService,
        SnapshotSignalAnalyzer signalAnalyzer,
        SnapshotPathService pathService)
    {
        _schemaService = schemaService;
        _signalAnalyzer = signalAnalyzer;
        _pathService = pathService;
    }

    public SnapshotExtractorService() : this(
        new SnapshotSchemaService(),
        new SnapshotSignalAnalyzer(),
        new SnapshotPathService())
    {
    }

    /// <summary>
    /// Extracts all classes, methods, and signals from a solution and writes to the database.
    /// This method is idempotent - running it multiple times refreshes the data.
    /// </summary>
    public async Task ExtractSolutionAsync(string dbPath, string solutionPath)
    {
        // Ensure database is initialized
        _schemaService.InitializeDatabase(dbPath, solutionPath);

        // Load the solution
        var solution = await WorkspaceService.GetSolutionAsync(solutionPath);

        // Clear existing data for idempotency
        ClearExistingData(dbPath);

        // Process all projects
        foreach (var project in solution.Projects)
        {
            if (project.Language != LanguageNames.CSharp)
                continue;

            var compilation = await project.GetCompilationAsync();
            if (compilation == null)
                continue;

            // Process each document
            foreach (var document in project.Documents)
            {
                var syntaxTree = await document.GetSyntaxTreeAsync();
                if (syntaxTree == null)
                    continue;

                var semanticModel = compilation.GetSemanticModel(syntaxTree);
                var root = await syntaxTree.GetRootAsync();

                // Extract classes
                var classes = root.DescendantNodes().OfType<ClassDeclarationSyntax>().ToList();
                foreach (var classDecl in classes)
                {
                    var classRow = ExtractClass(document, classDecl, semanticModel);
                    PersistClass(dbPath, classRow);

                    // Extract methods for this class
                    var methods = classDecl.DescendantNodes().OfType<MethodDeclarationSyntax>();
                    foreach (var method in methods)
                    {
                        var methodRow = ExtractMethod(classRow.Id, method, semanticModel);
                        PersistMethod(dbPath, methodRow);

                        // Extract method calls
                        var calls = ExtractCalls(methodRow.Id, method, semanticModel);
                        foreach (var call in calls)
                        {
                            PersistCall(dbPath, call);
                        }
                    }

                    // Extract dependencies
                    var deps = ExtractDependencies(classRow.Id, classDecl, semanticModel);
                    foreach (var dep in deps)
                    {
                        PersistDependency(dbPath, dep);
                    }

                    // Extract annotations
                    var annotations = ExtractAnnotations(classRow.Id, classDecl);
                    foreach (var annotation in annotations)
                    {
                        PersistAnnotation(dbPath, annotation);
                    }
                }
            }
        }
    }

    private void ClearExistingData(string dbPath)
    {
        using var connection = new SqliteConnection($"Data Source={dbPath}");
        connection.Open();

        // Delete in correct order to respect foreign keys
        var tables = new[] { "flags", "annotations", "calls", "dependencies", "methods", "classes" };
        
        foreach (var table in tables)
        {
            using var command = connection.CreateCommand();
            command.CommandText = $"DELETE FROM {table} WHERE snapshot_id = 1";
            command.ExecuteNonQuery();
        }
    }

    private SnapshotClassRow ExtractClass(Document document, ClassDeclarationSyntax classDecl, SemanticModel semanticModel)
    {
        var className = classDecl.Identifier.Text;
        var namespaceName = GetNamespace(classDecl);

        // Get signals from analyzer
        var signals = _signalAnalyzer.AnalyzeClass(classDecl, semanticModel);

        var location = classDecl.GetLocation();
        var lineSpan = location.GetLineSpan();

        var row = new SnapshotClassRow
        {
            SnapshotId = 1,
            Namespace = namespaceName,
            Name = className,
            Kind = classDecl.Keyword.Text,
            Accessibility = classDecl.Modifiers.ToString(),
            IsAbstract = classDecl.Modifiers.Any(SyntaxKind.AbstractKeyword),
            IsSealed = classDecl.Modifiers.Any(SyntaxKind.SealedKeyword),
            IsStatic = classDecl.Modifiers.Any(SyntaxKind.StaticKeyword),
            FilePath = document.FilePath ?? "",
            StartLine = lineSpan.StartLinePosition.Line + 1, // 1-indexed
            EndLine = lineSpan.EndLinePosition.Line + 1,
            
            // Base types and interfaces
            BaseTypes = classDecl.BaseList?.Types
                .Select(t => semanticModel.GetTypeInfo(t.Type).Type?.Name)
                .Where(n => n != null)
                .Join(","),
            Implements = classDecl.BaseList?.Types
                .Where(t => t.Type is Microsoft.CodeAnalysis.CSharp.Syntax.SimpleBaseTypeSyntax)
                .Select(t => semanticModel.GetTypeInfo(t.Type).Type?.Name)
                .Where(n => n != null)
                .Join(","),

            // Signals
            ReturnsNull = signals.ReturnsNull,
            CognitiveComplexity = signals.CognitiveComplexity,
            HasTryCatch = signals.HasTryCatch,
            CallsExternal = signals.CallsExternal,
            AccessesDb = signals.AccessesDb,
            FiltersByTenant = signals.FiltersByTenant
        };

        return row;
    }

    private SnapshotMethodRow ExtractMethod(int classId, MethodDeclarationSyntax method, SemanticModel semanticModel)
    {
        var signals = _signalAnalyzer.AnalyzeMethod(method, semanticModel);

        var row = new SnapshotMethodRow
        {
            SnapshotId = 1,
            ClassId = classId,
            Name = method.Identifier.Text,
            ReturnType = method.ReturnType.ToString(),
            Accessibility = method.Modifiers.ToString(),
            IsVirtual = method.Modifiers.Any(SyntaxKind.VirtualKeyword),
            IsOverride = method.Modifiers.Any(SyntaxKind.OverrideKeyword),
            IsStatic = method.Modifiers.Any(SyntaxKind.StaticKeyword),
            IsAbstract = method.Modifiers.Any(SyntaxKind.AbstractKeyword),
            Parameters = method.ParameterList.Parameters
                .Select(p => $"{p.Type} {p.Identifier}")
                .Join(","),
            StartLine = method.GetLocation().GetLineSpan().StartLinePosition.Line + 1,
            EndLine = method.GetLocation().GetLineSpan().EndLinePosition.Line + 1,

            // Signals
            ReturnsNull = signals.ReturnsNull,
            CognitiveComplexity = signals.CognitiveComplexity,
            HasTryCatch = signals.HasTryCatch,
            CallsExternal = signals.CallsExternal,
            AccessesDb = signals.AccessesDb,
            FiltersByTenant = signals.FiltersByTenant
        };

        return row;
    }

    private List<SnapshotCallRow> ExtractCalls(int methodId, MethodDeclarationSyntax method, SemanticModel semanticModel)
    {
        var calls = new List<SnapshotCallRow>();

        if (method.Body == null)
            return calls;

        var invocations = method.Body.DescendantNodes().OfType<InvocationExpressionSyntax>();
        
        foreach (var invocation in invocations)
        {
            var symbol = semanticModel.GetSymbolInfo(invocation).Symbol;
            if (symbol?.ContainingType == null)
                continue;

            var targetClass = symbol.ContainingType;
            var targetMethod = symbol.Name;

            // Try to get namespace
            var targetNamespace = targetClass.ContainingNamespace?.Name ?? "";

            calls.Add(new SnapshotCallRow
            {
                SnapshotId = 1,
                CallerMethodId = methodId,
                TargetNamespace = targetNamespace,
                TargetClass = targetClass.Name,
                TargetMethod = targetMethod,
                LineNumber = invocation.GetLocation().GetLineSpan().StartLinePosition.Line + 1
            });
        }

        return calls;
    }

    private List<SnapshotDependencyRow> ExtractDependencies(int classId, ClassDeclarationSyntax classDecl, SemanticModel semanticModel)
    {
        var deps = new List<SnapshotDependencyRow>();

        // Get base types
        if (classDecl.BaseList != null)
        {
            foreach (var baseType in classDecl.BaseList.Types)
            {
                var typeInfo = semanticModel.GetTypeInfo(baseType.Type);
                if (typeInfo.Type != null)
                {
                    deps.Add(new SnapshotDependencyRow
                    {
                        SnapshotId = 1,
                        FromClassId = classId,
                        ToNamespace = typeInfo.Type.ContainingNamespace?.Name ?? "",
                        ToName = typeInfo.Type.Name,
                        Kind = "inheritance"
                    });
                }
            }
        }

        // Get type references in members
        var typeRefs = classDecl.DescendantNodes()
            .OfType<IdentifierNameSyntax>()
            .Select(i => semanticModel.GetTypeInfo(i).Type)
            .Where(t => t != null && t.SpecialType != SpecialType.None)
            .DistinctBy(t => t!.Name);

        foreach (var type in typeRefs)
        {
            if (type == null) continue;
            
            deps.Add(new SnapshotDependencyRow
            {
                SnapshotId = 1,
                FromClassId = classId,
                ToNamespace = type.ContainingNamespace?.Name ?? "",
                ToName = type.Name,
                Kind = "reference"
            });
        }

        return deps;
    }

    private List<SnapshotAnnotationRow> ExtractAnnotations(int classId, ClassDeclarationSyntax classDecl)
    {
        var annotations = new List<SnapshotAnnotationRow>();

        foreach (var attr in classDecl.AttributeLists.SelectMany(a => a.Attributes))
        {
            annotations.Add(new SnapshotAnnotationRow
            {
                SnapshotId = 1,
                TargetType = "class",
                TargetId = classId,
                AnnotationName = attr.Name.ToString(),
                AnnotationArgs = attr.ArgumentList?.Arguments.ToString()
            });
        }

        return annotations;
    }

    private string GetNamespace(ClassDeclarationSyntax classDecl)
    {
        var namespaceNode = classDecl.Ancestors().OfType<NamespaceDeclarationSyntax>().FirstOrDefault();
        return namespaceNode?.Name.ToString() ?? "global";
    }

    private void PersistClass(string dbPath, SnapshotClassRow row)
    {
        using var connection = new SqliteConnection($"Data Source={dbPath}");
        connection.Open();

        using var command = connection.CreateCommand();
        command.CommandText = @"
            INSERT INTO classes (
                snapshot_id, namespace, name, kind, accessibility,
                is_abstract, is_sealed, is_static, base_types, implements,
                file_path, start_line, end_line,
                returns_null, cognitive_complexity, has_try_catch,
                calls_external, accesses_db, filters_by_tenant
            ) VALUES (
                @snapshot_id, @namespace, @name, @kind, @accessibility,
                @is_abstract, @is_sealed, @is_static, @base_types, @implements,
                @file_path, @start_line, @end_line,
                @returns_null, @cognitive_complexity, @has_try_catch,
                @calls_external, @accesses_db, @filters_by_tenant
            );
            SELECT last_insert_rowid();";

        AddClassParameters(command, row);

        row.Id = Convert.ToInt32(command.ExecuteScalar());
    }

    private void PersistMethod(string dbPath, SnapshotMethodRow row)
    {
        using var connection = new SqliteConnection($"Data Source={dbPath}");
        connection.Open();

        using var command = connection.CreateCommand();
        command.CommandText = @"
            INSERT INTO methods (
                snapshot_id, class_id, name, return_type, accessibility,
                is_virtual, is_override, is_static, is_abstract, parameters,
                start_line, end_line,
                returns_null, cognitive_complexity, has_try_catch,
                calls_external, accesses_db, filters_by_tenant
            ) VALUES (
                @snapshot_id, @class_id, @name, @return_type, @accessibility,
                @is_virtual, @is_override, @is_static, @is_abstract, @parameters,
                @start_line, @end_line,
                @returns_null, @cognitive_complexity, @has_try_catch,
                @calls_external, @accesses_db, @filters_by_tenant
            );
            SELECT last_insert_rowid();";

        AddMethodParameters(command, row);

        row.Id = Convert.ToInt32(command.ExecuteScalar());
    }

    private void PersistCall(string dbPath, SnapshotCallRow row)
    {
        using var connection = new SqliteConnection($"Data Source={dbPath}");
        connection.Open();

        using var command = connection.CreateCommand();
        command.CommandText = @"
            INSERT INTO calls (
                snapshot_id, caller_method_id, target_namespace, target_class, target_method, line_number
            ) VALUES (
                @snapshot_id, @caller_method_id, @target_namespace, @target_class, @target_method, @line_number
            )";

        AddCallParameters(command, row);
        command.ExecuteNonQuery();
    }

    private void PersistDependency(string dbPath, SnapshotDependencyRow row)
    {
        using var connection = new SqliteConnection($"Data Source={dbPath}");
        connection.Open();

        using var command = connection.CreateCommand();
        command.CommandText = @"
            INSERT INTO dependencies (
                snapshot_id, from_class_id, to_namespace, to_name, kind
            ) VALUES (
                @snapshot_id, @from_class_id, @to_namespace, @to_name, @kind
            )";

        AddDependencyParameters(command, row);
        command.ExecuteNonQuery();
    }

    private void PersistAnnotation(string dbPath, SnapshotAnnotationRow row)
    {
        using var connection = new SqliteConnection($"Data Source={dbPath}");
        connection.Open();

        using var command = connection.CreateCommand();
        command.CommandText = @"
            INSERT INTO annotations (
                snapshot_id, target_type, target_id, annotation_name, annotation_args
            ) VALUES (
                @snapshot_id, @target_type, @target_id, @annotation_name, @annotation_args
            )";

        AddAnnotationParameters(command, row);
        command.ExecuteNonQuery();
    }

    private void AddClassParameters(SqliteCommand command, SnapshotClassRow row)
    {
        command.Parameters.AddWithValue("@snapshot_id", row.SnapshotId);
        command.Parameters.AddWithValue("@namespace", row.Namespace);
        command.Parameters.AddWithValue("@name", row.Name);
        command.Parameters.AddWithValue("@kind", row.Kind);
        command.Parameters.AddWithValue("@accessibility", row.Accessibility ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@is_abstract", row.IsAbstract ? 1 : 0);
        command.Parameters.AddWithValue("@is_sealed", row.IsSealed ? 1 : 0);
        command.Parameters.AddWithValue("@is_static", row.IsStatic ? 1 : 0);
        command.Parameters.AddWithValue("@base_types", row.BaseTypes ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@implements", row.Implements ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@file_path", row.FilePath);
        command.Parameters.AddWithValue("@start_line", row.StartLine);
        command.Parameters.AddWithValue("@end_line", row.EndLine);
        command.Parameters.AddWithValue("@returns_null", row.ReturnsNull ? 1 : 0);
        command.Parameters.AddWithValue("@cognitive_complexity", row.CognitiveComplexity);
        command.Parameters.AddWithValue("@has_try_catch", row.HasTryCatch ? 1 : 0);
        command.Parameters.AddWithValue("@calls_external", row.CallsExternal ? 1 : 0);
        command.Parameters.AddWithValue("@accesses_db", row.AccessesDb ? 1 : 0);
        command.Parameters.AddWithValue("@filters_by_tenant", row.FiltersByTenant ? 1 : 0);
    }

    private void AddMethodParameters(SqliteCommand command, SnapshotMethodRow row)
    {
        command.Parameters.AddWithValue("@snapshot_id", row.SnapshotId);
        command.Parameters.AddWithValue("@class_id", row.ClassId);
        command.Parameters.AddWithValue("@name", row.Name);
        command.Parameters.AddWithValue("@return_type", row.ReturnType ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@accessibility", row.Accessibility ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@is_virtual", row.IsVirtual ? 1 : 0);
        command.Parameters.AddWithValue("@is_override", row.IsOverride ? 1 : 0);
        command.Parameters.AddWithValue("@is_static", row.IsStatic ? 1 : 0);
        command.Parameters.AddWithValue("@is_abstract", row.IsAbstract ? 1 : 0);
        command.Parameters.AddWithValue("@parameters", row.Parameters ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@start_line", row.StartLine);
        command.Parameters.AddWithValue("@end_line", row.EndLine);
        command.Parameters.AddWithValue("@returns_null", row.ReturnsNull ? 1 : 0);
        command.Parameters.AddWithValue("@cognitive_complexity", row.CognitiveComplexity);
        command.Parameters.AddWithValue("@has_try_catch", row.HasTryCatch ? 1 : 0);
        command.Parameters.AddWithValue("@calls_external", row.CallsExternal ? 1 : 0);
        command.Parameters.AddWithValue("@accesses_db", row.AccessesDb ? 1 : 0);
        command.Parameters.AddWithValue("@filters_by_tenant", row.FiltersByTenant ? 1 : 0);
    }

    private void AddCallParameters(SqliteCommand command, SnapshotCallRow row)
    {
        command.Parameters.AddWithValue("@snapshot_id", row.SnapshotId);
        command.Parameters.AddWithValue("@caller_method_id", row.CallerMethodId);
        command.Parameters.AddWithValue("@target_namespace", row.TargetNamespace);
        command.Parameters.AddWithValue("@target_class", row.TargetClass);
        command.Parameters.AddWithValue("@target_method", row.TargetMethod);
        command.Parameters.AddWithValue("@line_number", row.LineNumber);
    }

    private void AddDependencyParameters(SqliteCommand command, SnapshotDependencyRow row)
    {
        command.Parameters.AddWithValue("@snapshot_id", row.SnapshotId);
        command.Parameters.AddWithValue("@from_class_id", row.FromClassId);
        command.Parameters.AddWithValue("@to_namespace", row.ToNamespace);
        command.Parameters.AddWithValue("@to_name", row.ToName);
        command.Parameters.AddWithValue("@kind", row.Kind);
    }

    private void AddAnnotationParameters(SqliteCommand command, SnapshotAnnotationRow row)
    {
        command.Parameters.AddWithValue("@snapshot_id", row.SnapshotId);
        command.Parameters.AddWithValue("@target_type", row.TargetType);
        command.Parameters.AddWithValue("@target_id", row.TargetId);
        command.Parameters.AddWithValue("@annotation_name", row.AnnotationName);
        command.Parameters.AddWithValue("@annotation_args", row.AnnotationArgs ?? (object)DBNull.Value);
    }
}

// Helper extensions
internal static class StringExtensions
{
    public static string Join(this IEnumerable<string> strings, string separator)
    {
        return string.Join(separator, strings.Where(s => !string.IsNullOrEmpty(s)));
    }
}
