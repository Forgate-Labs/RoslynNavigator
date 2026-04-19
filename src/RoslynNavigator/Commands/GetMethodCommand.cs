using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using RoslynNavigator.Models;
using RoslynNavigator.Services;

namespace RoslynNavigator.Commands;

public static class GetMethodCommand
{
    public static async Task<MethodResult> ExecuteAsync(string solutionPath, string? filePath, string? className, string methodName)
    {
        var solution = await WorkspaceService.GetSolutionAsync(solutionPath);

        // If file and class are provided, search in specific location
        if (!string.IsNullOrEmpty(filePath))
        {
            var document = await WorkspaceService.FindDocumentAsync(solution, filePath, solutionPath);
            if (document == null)
                throw new FileNotFoundException($"File not found: {filePath}");

            var result = await FindMethodInDocument(document, methodName, className, solutionPath);
            if (result != null)
                return result;

            throw new InvalidOperationException($"Method '{methodName}' not found in file {filePath}");
        }

        // Search across entire solution
        foreach (var project in solution.Projects)
        {
            foreach (var document in project.Documents)
            {
                var result = await FindMethodInDocument(document, methodName, className, solutionPath);
                if (result != null)
                    return result;
            }
        }

        throw new InvalidOperationException($"Method '{methodName}' not found in solution");
    }

    private static async Task<MethodResult?> FindMethodInDocument(
        Microsoft.CodeAnalysis.Document document,
        string methodName,
        string? className,
        string solutionPath)
    {
        if (document.FilePath == null) return null;

        var syntaxRoot = await document.GetSyntaxRootAsync();
        if (syntaxRoot == null) return null;

        var methods = syntaxRoot.DescendantNodes()
            .OfType<MethodDeclarationSyntax>()
            .Where(m => m.Identifier.Text.Equals(methodName, StringComparison.OrdinalIgnoreCase));

        if (!string.IsNullOrEmpty(className))
        {
            methods = methods.Where(m =>
            {
                var containingClass = RoslynAnalyzer.GetContainingClassName(m);
                return containingClass != null &&
                       containingClass.Equals(className, StringComparison.OrdinalIgnoreCase);
            });
        }

        var method = methods.FirstOrDefault();
        if (method == null) return null;

        var sourceText = await document.GetTextAsync();
        var methodSpan = method.FullSpan;
        var sourceCode = sourceText.GetSubText(methodSpan).ToString();

        var containingClassName = RoslynAnalyzer.GetContainingClassName(method) ?? "";

        return new MethodResult
        {
            MethodName = method.Identifier.Text,
            ClassName = containingClassName,
            LineRange = RoslynAnalyzer.GetLineRange(method),
            FilePath = WorkspaceService.GetRelativePath(document.FilePath, solutionPath),
            Signature = RoslynAnalyzer.GetMethodSignature(method),
            Accessibility = RoslynAnalyzer.GetAccessibility(method.Modifiers),
            IsAsync = method.Modifiers.Any(m => m.Kind() == SyntaxKind.AsyncKeyword),
            ReturnType = method.ReturnType.ToString(),
            Parameters = RoslynAnalyzer.GetParameters(method.ParameterList),
            SourceCode = sourceCode
        };
    }
}
