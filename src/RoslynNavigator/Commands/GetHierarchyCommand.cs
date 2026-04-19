using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using RoslynNavigator.Models;
using RoslynNavigator.Services;

namespace RoslynNavigator.Commands;

public static class GetHierarchyCommand
{
    public static async Task<HierarchyResult> ExecuteAsync(string solutionPath, string className)
    {
        if (string.IsNullOrEmpty(className))
            throw new ArgumentException("Class name is required");

        var solution = await WorkspaceService.GetSolutionAsync(solutionPath);

        // First, find the class and get its symbol
        INamedTypeSymbol? targetClassSymbol = null;
        TypeDeclarationSyntax? targetClassNode = null;
        string? targetFilePath = null;

        foreach (var project in solution.Projects)
        {
            var compilation = await project.GetCompilationAsync();
            if (compilation == null) continue;

            foreach (var tree in compilation.SyntaxTrees)
            {
                var semanticModel = compilation.GetSemanticModel(tree);
                var root = await tree.GetRootAsync();

                var classDecl = root.DescendantNodes()
                    .OfType<TypeDeclarationSyntax>()
                    .FirstOrDefault(c => c.Identifier.Text.Equals(className, StringComparison.OrdinalIgnoreCase));

                if (classDecl != null)
                {
                    targetClassSymbol = semanticModel.GetDeclaredSymbol(classDecl) as INamedTypeSymbol;
                    targetClassNode = classDecl;
                    targetFilePath = tree.FilePath;
                    if (targetClassSymbol != null) break;
                }
            }
            if (targetClassSymbol != null) break;
        }

        if (targetClassSymbol == null || targetClassNode == null || targetFilePath == null)
        {
            throw new InvalidOperationException($"Class '{className}' not found in solution");
        }

        // Get base types (inheritance chain)
        var baseTypes = new List<string>();
        var currentBase = targetClassSymbol.BaseType;
        while (currentBase != null && currentBase.SpecialType != SpecialType.System_Object)
        {
            baseTypes.Add(currentBase.Name);
            currentBase = currentBase.BaseType;
        }
        baseTypes.Add("object");

        // Get interfaces
        var interfaces = targetClassSymbol.AllInterfaces
            .Select(i => i.Name)
            .Distinct()
            .ToList();

        // Find derived types
        var derivedTypes = new List<DerivedTypeInfo>();
        foreach (var project in solution.Projects)
        {
            var compilation = await project.GetCompilationAsync();
            if (compilation == null) continue;

            foreach (var tree in compilation.SyntaxTrees)
            {
                var semanticModel = compilation.GetSemanticModel(tree);
                var root = await tree.GetRootAsync();

                // Check classes
                foreach (var classDecl in root.DescendantNodes().OfType<ClassDeclarationSyntax>())
                {
                    var classSymbol = semanticModel.GetDeclaredSymbol(classDecl) as INamedTypeSymbol;
                    if (classSymbol == null) continue;

                    if (IsDerivedFrom(classSymbol, targetClassSymbol))
                    {
                        derivedTypes.Add(new DerivedTypeInfo
                        {
                            Name = classDecl.Identifier.Text,
                            Kind = "class",
                            FilePath = WorkspaceService.GetRelativePath(tree.FilePath ?? "", solutionPath),
                            Line = RoslynAnalyzer.GetLine(classDecl),
                            Namespace = RoslynAnalyzer.GetNamespace(classDecl)
                        });
                    }
                }

                // Check records
                foreach (var recordDecl in root.DescendantNodes().OfType<RecordDeclarationSyntax>())
                {
                    var recordSymbol = semanticModel.GetDeclaredSymbol(recordDecl) as INamedTypeSymbol;
                    if (recordSymbol == null) continue;

                    if (IsDerivedFrom(recordSymbol, targetClassSymbol))
                    {
                        derivedTypes.Add(new DerivedTypeInfo
                        {
                            Name = recordDecl.Identifier.Text,
                            Kind = "record",
                            FilePath = WorkspaceService.GetRelativePath(tree.FilePath ?? "", solutionPath),
                            Line = RoslynAnalyzer.GetLine(recordDecl),
                            Namespace = RoslynAnalyzer.GetNamespace(recordDecl)
                        });
                    }
                }
            }
        }

        return new HierarchyResult
        {
            ClassName = targetClassNode.Identifier.Text,
            FilePath = WorkspaceService.GetRelativePath(targetFilePath, solutionPath),
            Namespace = RoslynAnalyzer.GetNamespace(targetClassNode),
            BaseTypes = baseTypes,
            Interfaces = interfaces,
            DerivedTypes = derivedTypes
        };
    }

    private static bool IsDerivedFrom(INamedTypeSymbol typeSymbol, INamedTypeSymbol baseSymbol)
    {
        // Don't match the type itself
        if (SymbolEqualityComparer.Default.Equals(typeSymbol.OriginalDefinition, baseSymbol.OriginalDefinition) ||
            typeSymbol.OriginalDefinition.ToString() == baseSymbol.OriginalDefinition.ToString())
        {
            return false;
        }

        var current = typeSymbol.BaseType;
        while (current != null)
        {
            if (SymbolEqualityComparer.Default.Equals(current.OriginalDefinition, baseSymbol.OriginalDefinition) ||
                current.OriginalDefinition.ToString() == baseSymbol.OriginalDefinition.ToString())
            {
                return true;
            }
            current = current.BaseType;
        }
        return false;
    }
}
