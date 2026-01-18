using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using RoslynNavigator.Models;
using RoslynNavigator.Services;

namespace RoslynNavigator.Commands;

public static class FindImplementationsCommand
{
    public static async Task<ImplementationResult> ExecuteAsync(string solutionPath, string interfaceName)
    {
        if (string.IsNullOrEmpty(interfaceName))
            throw new ArgumentException("Interface name is required");

        var solution = await WorkspaceService.GetSolutionAsync(solutionPath);
        var implementations = new List<ImplementationInfo>();

        // First, find the interface symbol
        INamedTypeSymbol? interfaceSymbol = null;
        foreach (var project in solution.Projects)
        {
            var compilation = await project.GetCompilationAsync();
            if (compilation == null) continue;

            foreach (var tree in compilation.SyntaxTrees)
            {
                var semanticModel = compilation.GetSemanticModel(tree);
                var root = await tree.GetRootAsync();

                var interfaceDecl = root.DescendantNodes()
                    .OfType<InterfaceDeclarationSyntax>()
                    .FirstOrDefault(i => i.Identifier.Text.Equals(interfaceName, StringComparison.OrdinalIgnoreCase));

                if (interfaceDecl != null)
                {
                    interfaceSymbol = semanticModel.GetDeclaredSymbol(interfaceDecl) as INamedTypeSymbol;
                    if (interfaceSymbol != null) break;
                }
            }
            if (interfaceSymbol != null) break;
        }

        if (interfaceSymbol == null)
        {
            return new ImplementationResult
            {
                Interface = interfaceName,
                Implementations = implementations,
                TotalCount = 0
            };
        }

        // Now find all types that implement this interface
        foreach (var project in solution.Projects)
        {
            var compilation = await project.GetCompilationAsync();
            if (compilation == null) continue;

            foreach (var tree in compilation.SyntaxTrees)
            {
                var semanticModel = compilation.GetSemanticModel(tree);
                var root = await tree.GetRootAsync();

                // Check classes
                var classDeclarations = root.DescendantNodes()
                    .OfType<ClassDeclarationSyntax>();

                foreach (var classDecl in classDeclarations)
                {
                    var classSymbol = semanticModel.GetDeclaredSymbol(classDecl) as INamedTypeSymbol;
                    if (classSymbol == null) continue;

                    if (ImplementsInterface(classSymbol, interfaceSymbol))
                    {
                        implementations.Add(new ImplementationInfo
                        {
                            Name = classDecl.Identifier.Text,
                            Kind = "class",
                            FilePath = WorkspaceService.GetRelativePath(tree.FilePath ?? "", solutionPath),
                            Line = RoslynAnalyzer.GetLine(classDecl),
                            Namespace = RoslynAnalyzer.GetNamespace(classDecl)
                        });
                    }
                }

                // Check structs
                var structDeclarations = root.DescendantNodes()
                    .OfType<StructDeclarationSyntax>();

                foreach (var structDecl in structDeclarations)
                {
                    var structSymbol = semanticModel.GetDeclaredSymbol(structDecl) as INamedTypeSymbol;
                    if (structSymbol == null) continue;

                    if (ImplementsInterface(structSymbol, interfaceSymbol))
                    {
                        implementations.Add(new ImplementationInfo
                        {
                            Name = structDecl.Identifier.Text,
                            Kind = "struct",
                            FilePath = WorkspaceService.GetRelativePath(tree.FilePath ?? "", solutionPath),
                            Line = RoslynAnalyzer.GetLine(structDecl),
                            Namespace = RoslynAnalyzer.GetNamespace(structDecl)
                        });
                    }
                }

                // Check records
                var recordDeclarations = root.DescendantNodes()
                    .OfType<RecordDeclarationSyntax>();

                foreach (var recordDecl in recordDeclarations)
                {
                    var recordSymbol = semanticModel.GetDeclaredSymbol(recordDecl) as INamedTypeSymbol;
                    if (recordSymbol == null) continue;

                    if (ImplementsInterface(recordSymbol, interfaceSymbol))
                    {
                        implementations.Add(new ImplementationInfo
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

        return new ImplementationResult
        {
            Interface = interfaceName,
            Implementations = implementations,
            TotalCount = implementations.Count
        };
    }

    private static bool ImplementsInterface(INamedTypeSymbol typeSymbol, INamedTypeSymbol interfaceSymbol)
    {
        return typeSymbol.AllInterfaces.Any(i =>
            SymbolEqualityComparer.Default.Equals(i.OriginalDefinition, interfaceSymbol.OriginalDefinition) ||
            i.OriginalDefinition.ToString() == interfaceSymbol.OriginalDefinition.ToString());
    }
}
