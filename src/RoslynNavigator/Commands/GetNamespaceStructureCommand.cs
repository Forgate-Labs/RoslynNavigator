using Microsoft.CodeAnalysis.CSharp.Syntax;
using RoslynNavigator.Models;
using RoslynNavigator.Services;

namespace RoslynNavigator.Commands;

public static class GetNamespaceStructureCommand
{
    public static async Task<NamespaceStructureResult> ExecuteAsync(string solutionPath, string projectName)
    {
        var solution = await WorkspaceService.GetSolutionAsync(solutionPath);
        var project = WorkspaceService.FindProject(solution, projectName);

        if (project == null)
            throw new InvalidOperationException($"Project '{projectName}' not found in solution");

        var namespaceDict = new Dictionary<string, List<string>>();

        foreach (var document in project.Documents)
        {
            if (document.FilePath == null) continue;

            var syntaxRoot = await document.GetSyntaxRootAsync();
            if (syntaxRoot == null) continue;

            // Find all type declarations
            var typeDeclarations = syntaxRoot.DescendantNodes()
                .OfType<TypeDeclarationSyntax>();

            foreach (var typeDecl in typeDeclarations)
            {
                var ns = RoslynAnalyzer.GetNamespace(typeDecl);
                if (string.IsNullOrEmpty(ns)) ns = "(global)";

                if (!namespaceDict.ContainsKey(ns))
                {
                    namespaceDict[ns] = new List<string>();
                }

                var className = typeDecl.Identifier.Text;
                if (!namespaceDict[ns].Contains(className))
                {
                    namespaceDict[ns].Add(className);
                }
            }
        }

        var namespaces = namespaceDict
            .OrderBy(kvp => kvp.Key)
            .Select(kvp => new NamespaceInfo
            {
                Name = kvp.Key,
                ClassCount = kvp.Value.Count,
                Classes = kvp.Value.OrderBy(c => c).ToList()
            })
            .ToList();

        return new NamespaceStructureResult
        {
            ProjectName = projectName,
            Namespaces = namespaces
        };
    }
}
