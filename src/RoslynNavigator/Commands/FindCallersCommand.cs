using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using RoslynNavigator.Models;
using RoslynNavigator.Services;

namespace RoslynNavigator.Commands;

public static class FindCallersCommand
{
    public static async Task<CallersResult> ExecuteAsync(string solutionPath, string symbolName)
    {
        if (string.IsNullOrEmpty(symbolName))
            throw new ArgumentException("Symbol name is required (e.g., ClassName.MethodName)");

        var solution = await WorkspaceService.GetSolutionAsync(solutionPath);
        var callers = new List<CallerInfo>();

        // Parse symbol name (e.g., "ClassName.MethodName")
        var parts = symbolName.Split('.');
        var targetMethodName = parts[^1];
        var targetClassName = parts.Length > 1 ? parts[^2] : null;

        // First, find the method symbol
        IMethodSymbol? targetMethodSymbol = null;
        foreach (var project in solution.Projects)
        {
            var compilation = await project.GetCompilationAsync();
            if (compilation == null) continue;

            foreach (var tree in compilation.SyntaxTrees)
            {
                var semanticModel = compilation.GetSemanticModel(tree);
                var root = await tree.GetRootAsync();

                var methodDecls = root.DescendantNodes()
                    .OfType<MethodDeclarationSyntax>()
                    .Where(m => m.Identifier.Text.Equals(targetMethodName, StringComparison.OrdinalIgnoreCase));

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
                    targetMethodSymbol = semanticModel.GetDeclaredSymbol(methodDecl) as IMethodSymbol;
                    if (targetMethodSymbol != null) break;
                }
            }
            if (targetMethodSymbol != null) break;
        }

        if (targetMethodSymbol == null)
        {
            return new CallersResult
            {
                Symbol = symbolName,
                Callers = callers,
                TotalCount = 0
            };
        }

        // Now find all invocations of this method
        foreach (var project in solution.Projects)
        {
            var compilation = await project.GetCompilationAsync();
            if (compilation == null) continue;

            foreach (var tree in compilation.SyntaxTrees)
            {
                var sourceText = await tree.GetTextAsync();

                // Pre-filter: check if file contains the method name
                if (!sourceText.ToString().Contains(targetMethodName, StringComparison.OrdinalIgnoreCase))
                    continue;

                var semanticModel = compilation.GetSemanticModel(tree);
                var root = await tree.GetRootAsync();

                // Find all invocation expressions
                var invocations = root.DescendantNodes()
                    .OfType<InvocationExpressionSyntax>();

                foreach (var invocation in invocations)
                {
                    var symbolInfo = semanticModel.GetSymbolInfo(invocation);
                    var invokedSymbol = symbolInfo.Symbol ?? symbolInfo.CandidateSymbols.FirstOrDefault();

                    if (invokedSymbol is IMethodSymbol invokedMethod)
                    {
                        if (SymbolsMatch(targetMethodSymbol, invokedMethod))
                        {
                            var lineSpan = invocation.GetLocation().GetLineSpan();
                            var line = lineSpan.StartLinePosition.Line;
                            var lineText = sourceText.Lines[line].ToString();

                            var callerMethod = RoslynAnalyzer.GetContainingMethodName(invocation) ?? "(top-level)";
                            var callerClass = RoslynAnalyzer.GetContainingClassName(invocation) ?? "(global)";

                            // Avoid listing the method as calling itself (unless it's actually recursive)
                            var isDefinitionSite = callerClass.Equals(targetClassName, StringComparison.OrdinalIgnoreCase) &&
                                                   callerMethod.Equals(targetMethodName, StringComparison.OrdinalIgnoreCase) &&
                                                   invocation.Parent is not InvocationExpressionSyntax;

                            callers.Add(new CallerInfo
                            {
                                CallerClass = callerClass,
                                CallerMethod = callerMethod,
                                FilePath = WorkspaceService.GetRelativePath(tree.FilePath ?? "", solutionPath),
                                Line = line + 1,
                                ContextCode = lineText.Trim()
                            });
                        }
                    }
                }
            }
        }

        return new CallersResult
        {
            Symbol = symbolName,
            Callers = callers,
            TotalCount = callers.Count
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
