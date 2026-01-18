using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using RoslynNavigator.Models;
using RoslynNavigator.Services;

namespace RoslynNavigator.Commands;

public static class GetConstructorDepsCommand
{
    public static async Task<ConstructorDepsResult> ExecuteAsync(string solutionPath, string className)
    {
        if (string.IsNullOrEmpty(className))
            throw new ArgumentException("Class name is required");

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

                var semanticModel = compilation.GetSemanticModel(syntaxRoot.SyntaxTree);
                var constructors = new List<ConstructorInfo>();

                foreach (var ctor in classNode.Members.OfType<ConstructorDeclarationSyntax>())
                {
                    var parameters = new List<ConstructorParameterInfo>();

                    foreach (var param in ctor.ParameterList.Parameters)
                    {
                        var paramType = param.Type;
                        var fullTypeName = paramType?.ToString() ?? "var";

                        // Try to get the full type name from semantic model
                        if (paramType != null)
                        {
                            var typeInfo = semanticModel.GetTypeInfo(paramType);
                            if (typeInfo.Type != null)
                            {
                                fullTypeName = typeInfo.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
                                // Remove "global::" prefix for cleaner output
                                if (fullTypeName.StartsWith("global::"))
                                    fullTypeName = fullTypeName.Substring(8);
                            }
                        }

                        parameters.Add(new ConstructorParameterInfo
                        {
                            Name = param.Identifier.Text,
                            Type = param.Type?.ToString() ?? "var",
                            FullTypeName = fullTypeName
                        });
                    }

                    constructors.Add(new ConstructorInfo
                    {
                        Parameters = parameters,
                        LineRange = RoslynAnalyzer.GetLineRange(ctor),
                        Signature = RoslynAnalyzer.GetConstructorSignature(ctor)
                    });
                }

                if (constructors.Count > 0 || classNode is ClassDeclarationSyntax)
                {
                    return new ConstructorDepsResult
                    {
                        ClassName = classNode.Identifier.Text,
                        FilePath = WorkspaceService.GetRelativePath(document.FilePath, solutionPath),
                        Namespace = RoslynAnalyzer.GetNamespace(classNode),
                        Constructors = constructors
                    };
                }
            }
        }

        throw new InvalidOperationException($"Class '{className}' not found in solution");
    }
}
