using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using RoslynNavigator.Models;
using RoslynNavigator.Services;

namespace RoslynNavigator.Commands;

public static class FindInterfaceConsumersCommand
{
    public static async Task<InterfaceConsumersResult> ExecuteAsync(string solutionPath, string interfaceName)
    {
        if (string.IsNullOrEmpty(interfaceName))
            throw new ArgumentException("Interface name is required");

        var solution = await WorkspaceService.GetSolutionAsync(solutionPath);
        var implementations = new List<ImplementationInfo>();
        var injections = new List<InjectionInfo>();

        string? definedIn = null;
        int? definitionLine = null;
        INamedTypeSymbol? interfaceSymbol = null;

        // First, find the interface symbol and its definition
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
                    if (interfaceSymbol != null)
                    {
                        definedIn = WorkspaceService.GetRelativePath(tree.FilePath ?? "", solutionPath);
                        definitionLine = RoslynAnalyzer.GetLine(interfaceDecl);
                        break;
                    }
                }
            }
            if (interfaceSymbol != null) break;
        }

        if (interfaceSymbol == null)
        {
            return new InterfaceConsumersResult
            {
                Interface = interfaceName,
                DefinedIn = null,
                DefinitionLine = null,
                Implementations = implementations,
                Injections = injections
            };
        }

        // Find implementations and injections
        foreach (var project in solution.Projects)
        {
            var compilation = await project.GetCompilationAsync();
            if (compilation == null) continue;

            foreach (var tree in compilation.SyntaxTrees)
            {
                var semanticModel = compilation.GetSemanticModel(tree);
                var root = await tree.GetRootAsync();
                var filePath = WorkspaceService.GetRelativePath(tree.FilePath ?? "", solutionPath);

                // Find implementations (classes, structs, records)
                FindImplementationsInTree(root, semanticModel, interfaceSymbol, filePath, implementations);

                // Find injections (constructor parameters, fields, properties)
                FindInjectionsInTree(root, semanticModel, interfaceSymbol, filePath, injections);
            }
        }

        return new InterfaceConsumersResult
        {
            Interface = interfaceName,
            DefinedIn = definedIn,
            DefinitionLine = definitionLine,
            Implementations = implementations,
            Injections = injections
        };
    }

    private static void FindImplementationsInTree(
        SyntaxNode root,
        SemanticModel semanticModel,
        INamedTypeSymbol interfaceSymbol,
        string filePath,
        List<ImplementationInfo> implementations)
    {
        // Check classes
        foreach (var classDecl in root.DescendantNodes().OfType<ClassDeclarationSyntax>())
        {
            var classSymbol = semanticModel.GetDeclaredSymbol(classDecl) as INamedTypeSymbol;
            if (classSymbol != null && ImplementsInterface(classSymbol, interfaceSymbol))
            {
                implementations.Add(new ImplementationInfo
                {
                    Name = classDecl.Identifier.Text,
                    Kind = "class",
                    FilePath = filePath,
                    Line = RoslynAnalyzer.GetLine(classDecl),
                    Namespace = RoslynAnalyzer.GetNamespace(classDecl)
                });
            }
        }

        // Check structs
        foreach (var structDecl in root.DescendantNodes().OfType<StructDeclarationSyntax>())
        {
            var structSymbol = semanticModel.GetDeclaredSymbol(structDecl) as INamedTypeSymbol;
            if (structSymbol != null && ImplementsInterface(structSymbol, interfaceSymbol))
            {
                implementations.Add(new ImplementationInfo
                {
                    Name = structDecl.Identifier.Text,
                    Kind = "struct",
                    FilePath = filePath,
                    Line = RoslynAnalyzer.GetLine(structDecl),
                    Namespace = RoslynAnalyzer.GetNamespace(structDecl)
                });
            }
        }

        // Check records
        foreach (var recordDecl in root.DescendantNodes().OfType<RecordDeclarationSyntax>())
        {
            var recordSymbol = semanticModel.GetDeclaredSymbol(recordDecl) as INamedTypeSymbol;
            if (recordSymbol != null && ImplementsInterface(recordSymbol, interfaceSymbol))
            {
                implementations.Add(new ImplementationInfo
                {
                    Name = recordDecl.Identifier.Text,
                    Kind = "record",
                    FilePath = filePath,
                    Line = RoslynAnalyzer.GetLine(recordDecl),
                    Namespace = RoslynAnalyzer.GetNamespace(recordDecl)
                });
            }
        }
    }

    private static void FindInjectionsInTree(
        SyntaxNode root,
        SemanticModel semanticModel,
        INamedTypeSymbol interfaceSymbol,
        string filePath,
        List<InjectionInfo> injections)
    {
        // Check constructor parameters
        foreach (var ctor in root.DescendantNodes().OfType<ConstructorDeclarationSyntax>())
        {
            var className = RoslynAnalyzer.GetContainingClassName(ctor);
            if (className == null) continue;

            foreach (var param in ctor.ParameterList.Parameters)
            {
                if (param.Type == null) continue;

                var typeInfo = semanticModel.GetTypeInfo(param.Type);
                if (typeInfo.Type is INamedTypeSymbol paramType && MatchesInterface(paramType, interfaceSymbol))
                {
                    injections.Add(new InjectionInfo
                    {
                        ClassName = className,
                        MemberName = param.Identifier.Text,
                        MemberType = "constructor-parameter",
                        FilePath = filePath,
                        Line = RoslynAnalyzer.GetLine(param)
                    });
                }
            }
        }

        // Check fields
        foreach (var field in root.DescendantNodes().OfType<FieldDeclarationSyntax>())
        {
            var className = RoslynAnalyzer.GetContainingClassName(field);
            if (className == null) continue;

            var typeInfo = semanticModel.GetTypeInfo(field.Declaration.Type);
            if (typeInfo.Type is INamedTypeSymbol fieldType && MatchesInterface(fieldType, interfaceSymbol))
            {
                foreach (var variable in field.Declaration.Variables)
                {
                    injections.Add(new InjectionInfo
                    {
                        ClassName = className,
                        MemberName = variable.Identifier.Text,
                        MemberType = "field",
                        FilePath = filePath,
                        Line = RoslynAnalyzer.GetLine(field)
                    });
                }
            }
        }

        // Check properties
        foreach (var property in root.DescendantNodes().OfType<PropertyDeclarationSyntax>())
        {
            var className = RoslynAnalyzer.GetContainingClassName(property);
            if (className == null) continue;

            var typeInfo = semanticModel.GetTypeInfo(property.Type);
            if (typeInfo.Type is INamedTypeSymbol propType && MatchesInterface(propType, interfaceSymbol))
            {
                injections.Add(new InjectionInfo
                {
                    ClassName = className,
                    MemberName = property.Identifier.Text,
                    MemberType = "property",
                    FilePath = filePath,
                    Line = RoslynAnalyzer.GetLine(property)
                });
            }
        }
    }

    private static bool ImplementsInterface(INamedTypeSymbol typeSymbol, INamedTypeSymbol interfaceSymbol)
    {
        return typeSymbol.AllInterfaces.Any(i =>
            SymbolEqualityComparer.Default.Equals(i.OriginalDefinition, interfaceSymbol.OriginalDefinition) ||
            i.OriginalDefinition.ToString() == interfaceSymbol.OriginalDefinition.ToString());
    }

    private static bool MatchesInterface(INamedTypeSymbol typeSymbol, INamedTypeSymbol interfaceSymbol)
    {
        // Direct match
        if (SymbolEqualityComparer.Default.Equals(typeSymbol.OriginalDefinition, interfaceSymbol.OriginalDefinition) ||
            typeSymbol.OriginalDefinition.ToString() == interfaceSymbol.OriginalDefinition.ToString())
        {
            return true;
        }

        // Check if it's a generic type containing the interface (e.g., IEnumerable<IMyInterface>)
        if (typeSymbol.TypeArguments.Any(arg =>
            arg is INamedTypeSymbol argType &&
            (SymbolEqualityComparer.Default.Equals(argType.OriginalDefinition, interfaceSymbol.OriginalDefinition) ||
             argType.OriginalDefinition.ToString() == interfaceSymbol.OriginalDefinition.ToString())))
        {
            return true;
        }

        return false;
    }
}
