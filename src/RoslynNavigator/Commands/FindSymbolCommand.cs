using Microsoft.CodeAnalysis.CSharp.Syntax;
using RoslynNavigator.Models;
using RoslynNavigator.Services;

namespace RoslynNavigator.Commands;

public static class FindSymbolCommand
{
    public static async Task<SymbolSearchResult> ExecuteAsync(string solutionPath, string name, string? kind)
    {
        var solution = await WorkspaceService.GetSolutionAsync(solutionPath);
        var results = new List<SymbolLocation>();

        foreach (var project in solution.Projects)
        {
            foreach (var document in project.Documents)
            {
                if (document.FilePath == null) continue;

                var syntaxRoot = await document.GetSyntaxRootAsync();
                if (syntaxRoot == null) continue;

                // Find classes
                if (kind == null || kind.Equals("class", StringComparison.OrdinalIgnoreCase))
                {
                    var classes = syntaxRoot.DescendantNodes()
                        .OfType<ClassDeclarationSyntax>()
                        .Where(c => c.Identifier.Text.Equals(name, StringComparison.OrdinalIgnoreCase));

                    foreach (var classDecl in classes)
                    {
                        results.Add(new SymbolLocation
                        {
                            FilePath = WorkspaceService.GetRelativePath(document.FilePath, solutionPath),
                            LineRange = RoslynAnalyzer.GetLineRange(classDecl),
                            Namespace = RoslynAnalyzer.GetNamespace(classDecl),
                            FullName = $"{RoslynAnalyzer.GetNamespace(classDecl)}.{classDecl.Identifier.Text}"
                        });
                    }
                }

                // Find structs
                if (kind == null || kind.Equals("struct", StringComparison.OrdinalIgnoreCase))
                {
                    var structs = syntaxRoot.DescendantNodes()
                        .OfType<StructDeclarationSyntax>()
                        .Where(s => s.Identifier.Text.Equals(name, StringComparison.OrdinalIgnoreCase));

                    foreach (var structDecl in structs)
                    {
                        results.Add(new SymbolLocation
                        {
                            FilePath = WorkspaceService.GetRelativePath(document.FilePath, solutionPath),
                            LineRange = RoslynAnalyzer.GetLineRange(structDecl),
                            Namespace = RoslynAnalyzer.GetNamespace(structDecl),
                            FullName = $"{RoslynAnalyzer.GetNamespace(structDecl)}.{structDecl.Identifier.Text}"
                        });
                    }
                }

                // Find interfaces
                if (kind == null || kind.Equals("interface", StringComparison.OrdinalIgnoreCase))
                {
                    var interfaces = syntaxRoot.DescendantNodes()
                        .OfType<InterfaceDeclarationSyntax>()
                        .Where(i => i.Identifier.Text.Equals(name, StringComparison.OrdinalIgnoreCase));

                    foreach (var interfaceDecl in interfaces)
                    {
                        results.Add(new SymbolLocation
                        {
                            FilePath = WorkspaceService.GetRelativePath(document.FilePath, solutionPath),
                            LineRange = RoslynAnalyzer.GetLineRange(interfaceDecl),
                            Namespace = RoslynAnalyzer.GetNamespace(interfaceDecl),
                            FullName = $"{RoslynAnalyzer.GetNamespace(interfaceDecl)}.{interfaceDecl.Identifier.Text}"
                        });
                    }
                }

                // Find methods
                if (kind == null || kind.Equals("method", StringComparison.OrdinalIgnoreCase))
                {
                    var methods = syntaxRoot.DescendantNodes()
                        .OfType<MethodDeclarationSyntax>()
                        .Where(m => m.Identifier.Text.Equals(name, StringComparison.OrdinalIgnoreCase));

                    foreach (var method in methods)
                    {
                        var className = RoslynAnalyzer.GetContainingClassName(method) ?? "";
                        var ns = RoslynAnalyzer.GetNamespace(method);
                        results.Add(new SymbolLocation
                        {
                            FilePath = WorkspaceService.GetRelativePath(document.FilePath, solutionPath),
                            LineRange = RoslynAnalyzer.GetLineRange(method),
                            Namespace = ns,
                            FullName = $"{ns}.{className}.{method.Identifier.Text}"
                        });
                    }
                }

                // Find properties
                if (kind == null || kind.Equals("property", StringComparison.OrdinalIgnoreCase))
                {
                    var properties = syntaxRoot.DescendantNodes()
                        .OfType<PropertyDeclarationSyntax>()
                        .Where(p => p.Identifier.Text.Equals(name, StringComparison.OrdinalIgnoreCase));

                    foreach (var property in properties)
                    {
                        var className = RoslynAnalyzer.GetContainingClassName(property) ?? "";
                        var ns = RoslynAnalyzer.GetNamespace(property);
                        results.Add(new SymbolLocation
                        {
                            FilePath = WorkspaceService.GetRelativePath(document.FilePath, solutionPath),
                            LineRange = new[] { RoslynAnalyzer.GetLine(property), RoslynAnalyzer.GetLine(property) },
                            Namespace = ns,
                            FullName = $"{ns}.{className}.{property.Identifier.Text}"
                        });
                    }
                }
            }
        }

        return new SymbolSearchResult
        {
            SymbolName = name,
            Kind = kind ?? "any",
            Results = results
        };
    }
}
