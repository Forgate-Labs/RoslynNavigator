using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using RoslynNavigator.Models;
using RoslynNavigator.Services;

namespace RoslynNavigator.Commands;

public static class FindInstantiationsCommand
{
    public static async Task<InstantiationResult> ExecuteAsync(string solutionPath, string className)
    {
        if (string.IsNullOrEmpty(className))
            throw new ArgumentException("Class name is required");

        var solution = await WorkspaceService.GetSolutionAsync(solutionPath);
        var instantiations = new List<InstantiationInfo>();

        // First, find the class symbol
        INamedTypeSymbol? targetClassSymbol = null;
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
                    if (targetClassSymbol != null) break;
                }
            }
            if (targetClassSymbol != null) break;
        }

        if (targetClassSymbol == null)
        {
            return new InstantiationResult
            {
                ClassName = className,
                Instantiations = instantiations,
                TotalCount = 0
            };
        }

        // Now find all instantiations
        foreach (var project in solution.Projects)
        {
            var compilation = await project.GetCompilationAsync();
            if (compilation == null) continue;

            foreach (var tree in compilation.SyntaxTrees)
            {
                var sourceText = await tree.GetTextAsync();

                // Pre-filter: check if file contains the class name
                if (!sourceText.ToString().Contains(className, StringComparison.OrdinalIgnoreCase))
                    continue;

                var semanticModel = compilation.GetSemanticModel(tree);
                var root = await tree.GetRootAsync();

                // Find new ClassName(...) expressions
                var objectCreations = root.DescendantNodes()
                    .OfType<ObjectCreationExpressionSyntax>();

                foreach (var creation in objectCreations)
                {
                    var typeInfo = semanticModel.GetTypeInfo(creation);
                    if (typeInfo.Type == null) continue;

                    if (SymbolsMatch(targetClassSymbol, typeInfo.Type))
                    {
                        var lineSpan = creation.GetLocation().GetLineSpan();
                        var line = lineSpan.StartLinePosition.Line;
                        var lineText = sourceText.Lines[line].ToString();

                        instantiations.Add(new InstantiationInfo
                        {
                            FilePath = WorkspaceService.GetRelativePath(tree.FilePath ?? "", solutionPath),
                            Line = line + 1,
                            ContainingMethod = RoslynAnalyzer.GetContainingMethodName(creation) ?? "(top-level)",
                            ContainingClass = RoslynAnalyzer.GetContainingClassName(creation) ?? "(global)",
                            ContextCode = lineText.Trim()
                        });
                    }
                }

                // Find new(...) expressions (C# 9+ target-typed new)
                var implicitCreations = root.DescendantNodes()
                    .OfType<ImplicitObjectCreationExpressionSyntax>();

                foreach (var creation in implicitCreations)
                {
                    var typeInfo = semanticModel.GetTypeInfo(creation);
                    if (typeInfo.Type == null) continue;

                    if (SymbolsMatch(targetClassSymbol, typeInfo.Type))
                    {
                        var lineSpan = creation.GetLocation().GetLineSpan();
                        var line = lineSpan.StartLinePosition.Line;
                        var lineText = sourceText.Lines[line].ToString();

                        instantiations.Add(new InstantiationInfo
                        {
                            FilePath = WorkspaceService.GetRelativePath(tree.FilePath ?? "", solutionPath),
                            Line = line + 1,
                            ContainingMethod = RoslynAnalyzer.GetContainingMethodName(creation) ?? "(top-level)",
                            ContainingClass = RoslynAnalyzer.GetContainingClassName(creation) ?? "(global)",
                            ContextCode = lineText.Trim()
                        });
                    }
                }
            }
        }

        return new InstantiationResult
        {
            ClassName = className,
            Instantiations = instantiations,
            TotalCount = instantiations.Count
        };
    }

    private static bool SymbolsMatch(ISymbol target, ISymbol candidate)
    {
        var targetOriginal = target.OriginalDefinition;
        var candidateOriginal = candidate.OriginalDefinition;

        return SymbolEqualityComparer.Default.Equals(targetOriginal, candidateOriginal) ||
               targetOriginal.ToString() == candidateOriginal.ToString();
    }
}
