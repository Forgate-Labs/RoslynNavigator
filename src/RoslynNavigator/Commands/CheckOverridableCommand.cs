using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using RoslynNavigator.Models;
using RoslynNavigator.Services;

namespace RoslynNavigator.Commands;

public static class CheckOverridableCommand
{
    public static async Task<OverridableResult> ExecuteAsync(string solutionPath, string className, string methodName)
    {
        if (string.IsNullOrEmpty(className))
            throw new ArgumentException("Class name is required");
        if (string.IsNullOrEmpty(methodName))
            throw new ArgumentException("Method name is required");

        var solution = await WorkspaceService.GetSolutionAsync(solutionPath);

        foreach (var project in solution.Projects)
        {
            var compilation = await project.GetCompilationAsync();
            if (compilation == null) continue;

            foreach (var document in project.Documents)
            {
                if (document.FilePath == null) continue;

                var syntaxRoot = await document.GetSyntaxRootAsync();
                if (syntaxRoot == null) continue;

                var classNode = syntaxRoot.DescendantNodes()
                    .OfType<TypeDeclarationSyntax>()
                    .FirstOrDefault(c => c.Identifier.Text.Equals(className, StringComparison.OrdinalIgnoreCase));

                if (classNode == null) continue;

                var method = classNode.Members
                    .OfType<MethodDeclarationSyntax>()
                    .FirstOrDefault(m => m.Identifier.Text.Equals(methodName, StringComparison.OrdinalIgnoreCase));

                if (method == null) continue;

                var semanticModel = compilation.GetSemanticModel(syntaxRoot.SyntaxTree);
                var methodSymbol = semanticModel.GetDeclaredSymbol(method) as IMethodSymbol;

                var isVirtual = method.Modifiers.Any(SyntaxKind.VirtualKeyword);
                var isOverride = method.Modifiers.Any(SyntaxKind.OverrideKeyword);
                var isAbstract = method.Modifiers.Any(SyntaxKind.AbstractKeyword);
                var isSealed = method.Modifiers.Any(SyntaxKind.SealedKeyword);
                var isStatic = method.Modifiers.Any(SyntaxKind.StaticKeyword);

                // A method can be overridden if it's virtual, abstract, or override (but not sealed), and not static
                var canBeOverridden = !isStatic && !isSealed && (isVirtual || isAbstract || isOverride);

                string? baseMethod = null;
                if (isOverride && methodSymbol?.OverriddenMethod != null)
                {
                    var overriddenMethod = methodSymbol.OverriddenMethod;
                    baseMethod = $"{overriddenMethod.ContainingType.Name}.{overriddenMethod.Name}";
                }

                return new OverridableResult
                {
                    ClassName = classNode.Identifier.Text,
                    MethodName = method.Identifier.Text,
                    IsVirtual = isVirtual,
                    IsOverride = isOverride,
                    IsAbstract = isAbstract,
                    IsSealed = isSealed,
                    CanBeOverridden = canBeOverridden,
                    BaseMethod = baseMethod,
                    FilePath = WorkspaceService.GetRelativePath(document.FilePath, solutionPath),
                    Line = RoslynAnalyzer.GetLine(method)
                };
            }
        }

        throw new InvalidOperationException($"Method '{methodName}' not found in class '{className}'");
    }
}
