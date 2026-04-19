using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using RoslynNavigator.Models;
using RoslynNavigator.Services;

namespace RoslynNavigator.Commands;

public static class FindUsagesCommand
{
    public static async Task<UsageResult> ExecuteAsync(string solutionPath, string symbolName)
    {
        var solution = await WorkspaceService.GetSolutionAsync(solutionPath);
        var usages = new List<UsageInfo>();

        // Parse symbol name (e.g., "ClassName.MethodName" or just "MethodName")
        var parts = symbolName.Split('.');
        var targetName = parts[^1]; // Last part is the actual symbol name
        var targetClassName = parts.Length > 1 ? parts[^2] : null;

        // First, find the symbol definition to get its full qualified name
        ISymbol? targetSymbol = null;
        foreach (var project in solution.Projects)
        {
            var compilation = await project.GetCompilationAsync();
            if (compilation == null) continue;

            foreach (var tree in compilation.SyntaxTrees)
            {
                var semanticModel = compilation.GetSemanticModel(tree);
                var root = await tree.GetRootAsync();

                // Find methods matching the name
                var methodDecls = root.DescendantNodes()
                    .OfType<MethodDeclarationSyntax>()
                    .Where(m => m.Identifier.Text.Equals(targetName, StringComparison.OrdinalIgnoreCase));

                if (targetClassName != null)
                {
                    methodDecls = methodDecls.Where(m =>
                    {
                        var containingClass = RoslynAnalyzer.GetContainingClassName(m);
                        return containingClass != null &&
                               containingClass.Equals(targetClassName, StringComparison.OrdinalIgnoreCase);
                    });
                }

                var methodDecl = methodDecls.FirstOrDefault();
                if (methodDecl != null)
                {
                    targetSymbol = semanticModel.GetDeclaredSymbol(methodDecl);
                    if (targetSymbol != null) break;
                }

                // Find properties matching the name
                var propDecls = root.DescendantNodes()
                    .OfType<PropertyDeclarationSyntax>()
                    .Where(p => p.Identifier.Text.Equals(targetName, StringComparison.OrdinalIgnoreCase));

                if (targetClassName != null)
                {
                    propDecls = propDecls.Where(p =>
                    {
                        var containingClass = RoslynAnalyzer.GetContainingClassName(p);
                        return containingClass != null &&
                               containingClass.Equals(targetClassName, StringComparison.OrdinalIgnoreCase);
                    });
                }

                var propDecl = propDecls.FirstOrDefault();
                if (propDecl != null)
                {
                    targetSymbol = semanticModel.GetDeclaredSymbol(propDecl);
                    if (targetSymbol != null) break;
                }

                // Find classes matching the name
                if (targetClassName == null)
                {
                    var classDecl = root.DescendantNodes()
                        .OfType<ClassDeclarationSyntax>()
                        .FirstOrDefault(c => c.Identifier.Text.Equals(targetName, StringComparison.OrdinalIgnoreCase));

                    if (classDecl != null)
                    {
                        targetSymbol = semanticModel.GetDeclaredSymbol(classDecl);
                        if (targetSymbol != null) break;
                    }
                }
            }
            if (targetSymbol != null) break;
        }

        if (targetSymbol == null)
        {
            return new UsageResult
            {
                SymbolName = symbolName,
                TotalUsages = 0,
                Usages = usages
            };
        }

        // Now find all usages of this symbol
        foreach (var project in solution.Projects)
        {
            var compilation = await project.GetCompilationAsync();
            if (compilation == null) continue;

            foreach (var tree in compilation.SyntaxTrees)
            {
                // Pre-filter: check if file contains the target name (performance optimization)
                var sourceText = await tree.GetTextAsync();
                if (!sourceText.ToString().Contains(targetName, StringComparison.OrdinalIgnoreCase))
                    continue;

                var semanticModel = compilation.GetSemanticModel(tree);
                var root = await tree.GetRootAsync();

                // Find all identifier references
                var identifiers = root.DescendantNodes()
                    .OfType<IdentifierNameSyntax>()
                    .Where(i => i.Identifier.Text.Equals(targetName, StringComparison.OrdinalIgnoreCase));

                foreach (var identifier in identifiers)
                {
                    var symbolInfo = semanticModel.GetSymbolInfo(identifier);
                    var referencedSymbol = symbolInfo.Symbol ?? symbolInfo.CandidateSymbols.FirstOrDefault();

                    if (referencedSymbol != null && SymbolsMatch(targetSymbol, referencedSymbol))
                    {
                        var lineSpan = identifier.GetLocation().GetLineSpan();
                        var line = lineSpan.StartLinePosition.Line;
                        var lineText = sourceText.Lines[line].ToString();

                        usages.Add(new UsageInfo
                        {
                            FilePath = WorkspaceService.GetRelativePath(tree.FilePath ?? "", solutionPath),
                            Line = line + 1, // Convert to 1-based
                            Column = lineSpan.StartLinePosition.Character + 1,
                            ContextCode = lineText.Trim(),
                            MethodContext = RoslynAnalyzer.GetContainingMethodName(identifier) ?? "(top-level)"
                        });
                    }
                }

                // Also check object creation expressions for class instantiation
                if (targetSymbol is INamedTypeSymbol)
                {
                    var objectCreations = root.DescendantNodes()
                        .OfType<ObjectCreationExpressionSyntax>()
                        .Where(o => o.Type.ToString().Contains(targetName, StringComparison.OrdinalIgnoreCase));

                    foreach (var creation in objectCreations)
                    {
                        var typeInfo = semanticModel.GetTypeInfo(creation);
                        if (typeInfo.Type != null && SymbolsMatch(targetSymbol, typeInfo.Type))
                        {
                            var lineSpan = creation.GetLocation().GetLineSpan();
                            var line = lineSpan.StartLinePosition.Line;
                            var lineText = sourceText.Lines[line].ToString();

                            // Avoid duplicates
                            if (!usages.Any(u => u.FilePath == WorkspaceService.GetRelativePath(tree.FilePath ?? "", solutionPath) && u.Line == line + 1))
                            {
                                usages.Add(new UsageInfo
                                {
                                    FilePath = WorkspaceService.GetRelativePath(tree.FilePath ?? "", solutionPath),
                                    Line = line + 1,
                                    Column = lineSpan.StartLinePosition.Character + 1,
                                    ContextCode = lineText.Trim(),
                                    MethodContext = RoslynAnalyzer.GetContainingMethodName(creation) ?? "(top-level)"
                                });
                            }
                        }
                    }
                }
            }
        }

        return new UsageResult
        {
            SymbolName = symbolName,
            TotalUsages = usages.Count,
            Usages = usages
        };
    }

    private static bool SymbolsMatch(ISymbol target, ISymbol candidate)
    {
        // Compare by original definition to handle different compilation references
        var targetOriginal = target.OriginalDefinition;
        var candidateOriginal = candidate.OriginalDefinition;

        return SymbolEqualityComparer.Default.Equals(targetOriginal, candidateOriginal) ||
               targetOriginal.ToString() == candidateOriginal.ToString();
    }
}
