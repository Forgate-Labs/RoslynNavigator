using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using RoslynNavigator.Models;
using RoslynNavigator.Services;

namespace RoslynNavigator.Commands;

public static class ListClassesCommand
{
    public static async Task<ClassListResult> ExecuteAsync(string solutionPath, string namespaceName)
    {
        var solution = await WorkspaceService.GetSolutionAsync(solutionPath);
        var classes = new List<ClassInfo>();

        foreach (var project in solution.Projects)
        {
            foreach (var document in project.Documents)
            {
                if (document.FilePath == null) continue;

                var syntaxRoot = await document.GetSyntaxRootAsync();
                if (syntaxRoot == null) continue;

                // Find all type declarations in the target namespace
                var typeDeclarations = syntaxRoot.DescendantNodes()
                    .OfType<TypeDeclarationSyntax>()
                    .Where(t =>
                    {
                        var ns = RoslynAnalyzer.GetNamespace(t);
                        return ns.Equals(namespaceName, StringComparison.OrdinalIgnoreCase) ||
                               ns.StartsWith(namespaceName + ".", StringComparison.OrdinalIgnoreCase);
                    });

                foreach (var typeDecl in typeDeclarations)
                {
                    classes.Add(new ClassInfo
                    {
                        Name = typeDecl.Identifier.Text,
                        FilePath = WorkspaceService.GetRelativePath(document.FilePath, solutionPath),
                        LineRange = RoslynAnalyzer.GetLineRange(typeDecl),
                        Accessibility = RoslynAnalyzer.GetAccessibility(typeDecl.Modifiers),
                        IsStatic = typeDecl.Modifiers.Any(m => m.Kind() == SyntaxKind.StaticKeyword)
                    });
                }
            }
        }

        // Remove duplicates (partial classes may appear multiple times)
        classes = classes
            .GroupBy(c => c.Name)
            .Select(g => g.First())
            .OrderBy(c => c.Name)
            .ToList();

        return new ClassListResult
        {
            Namespace = namespaceName,
            TotalClasses = classes.Count,
            Classes = classes
        };
    }
}
