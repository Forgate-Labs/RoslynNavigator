using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using RoslynNavigator.Models;
using RoslynNavigator.Services;

namespace RoslynNavigator.Commands;

public static class GetMethodsCommand
{
    public static async Task<MethodsResult> ExecuteAsync(string solutionPath, string className, string methodNames)
    {
        if (string.IsNullOrEmpty(className))
            throw new ArgumentException("Class name is required");
        if (string.IsNullOrEmpty(methodNames))
            throw new ArgumentException("Method names are required (comma-separated)");

        var solution = await WorkspaceService.GetSolutionAsync(solutionPath);
        var targetMethods = methodNames.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        foreach (var project in solution.Projects)
        {
            foreach (var document in project.Documents)
            {
                if (document.FilePath == null) continue;

                var syntaxRoot = await document.GetSyntaxRootAsync();
                if (syntaxRoot == null) continue;

                var classNode = syntaxRoot.DescendantNodes()
                    .OfType<TypeDeclarationSyntax>()
                    .FirstOrDefault(c => c.Identifier.Text.Equals(className, StringComparison.OrdinalIgnoreCase));

                if (classNode == null) continue;

                var sourceText = await document.GetTextAsync();
                var foundMethods = new List<MethodInfo>();

                foreach (var targetMethod in targetMethods)
                {
                    var method = classNode.Members
                        .OfType<MethodDeclarationSyntax>()
                        .FirstOrDefault(m => m.Identifier.Text.Equals(targetMethod, StringComparison.OrdinalIgnoreCase));

                    if (method != null)
                    {
                        var methodSpan = method.FullSpan;
                        var sourceCode = sourceText.GetSubText(methodSpan).ToString();

                        foundMethods.Add(new MethodInfo
                        {
                            Name = method.Identifier.Text,
                            Signature = RoslynAnalyzer.GetMethodSignature(method),
                            LineRange = RoslynAnalyzer.GetLineRange(method),
                            SourceCode = sourceCode,
                            ReturnType = method.ReturnType.ToString(),
                            Parameters = RoslynAnalyzer.GetParameters(method.ParameterList),
                            Accessibility = RoslynAnalyzer.GetAccessibility(method.Modifiers),
                            IsAsync = method.Modifiers.Any(m => m.IsKind(SyntaxKind.AsyncKeyword))
                        });
                    }
                }

                if (foundMethods.Count > 0)
                {
                    return new MethodsResult
                    {
                        ClassName = classNode.Identifier.Text,
                        FilePath = WorkspaceService.GetRelativePath(document.FilePath, solutionPath),
                        Methods = foundMethods
                    };
                }
            }
        }

        throw new InvalidOperationException($"Class '{className}' not found in solution or no matching methods found");
    }
}
