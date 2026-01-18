using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using RoslynNavigator.Models;
using RoslynNavigator.Services;

namespace RoslynNavigator.Commands;

public static class FindByAttributeCommand
{
    public static async Task<AttributeSearchResult> ExecuteAsync(string solutionPath, string attributeName, string? pattern)
    {
        if (string.IsNullOrEmpty(attributeName))
            throw new ArgumentException("Attribute name is required");

        var solution = await WorkspaceService.GetSolutionAsync(solutionPath);
        var matches = new List<AttributeMatchInfo>();

        // Normalize attribute name (handle both "Obsolete" and "ObsoleteAttribute")
        var normalizedName = attributeName.EndsWith("Attribute", StringComparison.OrdinalIgnoreCase)
            ? attributeName
            : attributeName;
        var shortName = attributeName.EndsWith("Attribute", StringComparison.OrdinalIgnoreCase)
            ? attributeName.Substring(0, attributeName.Length - 9)
            : attributeName;

        foreach (var project in solution.Projects)
        {
            foreach (var document in project.Documents)
            {
                if (document.FilePath == null) continue;

                var syntaxRoot = await document.GetSyntaxRootAsync();
                if (syntaxRoot == null) continue;

                // Check methods
                foreach (var method in syntaxRoot.DescendantNodes().OfType<MethodDeclarationSyntax>())
                {
                    var matchingAttribute = FindMatchingAttribute(method.AttributeLists, normalizedName, shortName, pattern);
                    if (matchingAttribute != null)
                    {
                        matches.Add(new AttributeMatchInfo
                        {
                            MemberType = "method",
                            Name = method.Identifier.Text,
                            AttributeArguments = matchingAttribute,
                            FilePath = WorkspaceService.GetRelativePath(document.FilePath, solutionPath),
                            Line = RoslynAnalyzer.GetLine(method),
                            ContainingClass = RoslynAnalyzer.GetContainingClassName(method) ?? "(global)",
                            Namespace = RoslynAnalyzer.GetNamespace(method)
                        });
                    }
                }

                // Check classes
                foreach (var classDecl in syntaxRoot.DescendantNodes().OfType<ClassDeclarationSyntax>())
                {
                    var matchingAttribute = FindMatchingAttribute(classDecl.AttributeLists, normalizedName, shortName, pattern);
                    if (matchingAttribute != null)
                    {
                        matches.Add(new AttributeMatchInfo
                        {
                            MemberType = "class",
                            Name = classDecl.Identifier.Text,
                            AttributeArguments = matchingAttribute,
                            FilePath = WorkspaceService.GetRelativePath(document.FilePath, solutionPath),
                            Line = RoslynAnalyzer.GetLine(classDecl),
                            ContainingClass = RoslynAnalyzer.GetContainingClassName(classDecl) ?? "(none)",
                            Namespace = RoslynAnalyzer.GetNamespace(classDecl)
                        });
                    }
                }

                // Check properties
                foreach (var property in syntaxRoot.DescendantNodes().OfType<PropertyDeclarationSyntax>())
                {
                    var matchingAttribute = FindMatchingAttribute(property.AttributeLists, normalizedName, shortName, pattern);
                    if (matchingAttribute != null)
                    {
                        matches.Add(new AttributeMatchInfo
                        {
                            MemberType = "property",
                            Name = property.Identifier.Text,
                            AttributeArguments = matchingAttribute,
                            FilePath = WorkspaceService.GetRelativePath(document.FilePath, solutionPath),
                            Line = RoslynAnalyzer.GetLine(property),
                            ContainingClass = RoslynAnalyzer.GetContainingClassName(property) ?? "(global)",
                            Namespace = RoslynAnalyzer.GetNamespace(property)
                        });
                    }
                }

                // Check fields
                foreach (var field in syntaxRoot.DescendantNodes().OfType<FieldDeclarationSyntax>())
                {
                    var matchingAttribute = FindMatchingAttribute(field.AttributeLists, normalizedName, shortName, pattern);
                    if (matchingAttribute != null)
                    {
                        var fieldName = field.Declaration.Variables.FirstOrDefault()?.Identifier.Text ?? "(unknown)";
                        matches.Add(new AttributeMatchInfo
                        {
                            MemberType = "field",
                            Name = fieldName,
                            AttributeArguments = matchingAttribute,
                            FilePath = WorkspaceService.GetRelativePath(document.FilePath, solutionPath),
                            Line = RoslynAnalyzer.GetLine(field),
                            ContainingClass = RoslynAnalyzer.GetContainingClassName(field) ?? "(global)",
                            Namespace = RoslynAnalyzer.GetNamespace(field)
                        });
                    }
                }

                // Check parameters (useful for things like [FromBody], [FromQuery], etc.)
                foreach (var parameter in syntaxRoot.DescendantNodes().OfType<ParameterSyntax>())
                {
                    var matchingAttribute = FindMatchingAttribute(parameter.AttributeLists, normalizedName, shortName, pattern);
                    if (matchingAttribute != null)
                    {
                        var containingMethod = RoslynAnalyzer.GetContainingMethodName(parameter) ?? "(unknown)";
                        matches.Add(new AttributeMatchInfo
                        {
                            MemberType = "parameter",
                            Name = $"{containingMethod}.{parameter.Identifier.Text}",
                            AttributeArguments = matchingAttribute,
                            FilePath = WorkspaceService.GetRelativePath(document.FilePath, solutionPath),
                            Line = RoslynAnalyzer.GetLine(parameter),
                            ContainingClass = RoslynAnalyzer.GetContainingClassName(parameter) ?? "(global)",
                            Namespace = RoslynAnalyzer.GetNamespace(parameter)
                        });
                    }
                }
            }
        }

        return new AttributeSearchResult
        {
            Attribute = attributeName,
            Pattern = pattern,
            Matches = matches,
            TotalCount = matches.Count
        };
    }

    private static string? FindMatchingAttribute(
        SyntaxList<AttributeListSyntax> attributeLists,
        string normalizedName,
        string shortName,
        string? pattern)
    {
        foreach (var attrList in attributeLists)
        {
            foreach (var attr in attrList.Attributes)
            {
                var attrName = attr.Name.ToString();

                // Match attribute name (handle qualified names like [System.Obsolete])
                var nameMatches =
                    attrName.Equals(normalizedName, StringComparison.OrdinalIgnoreCase) ||
                    attrName.Equals(shortName, StringComparison.OrdinalIgnoreCase) ||
                    attrName.EndsWith("." + normalizedName, StringComparison.OrdinalIgnoreCase) ||
                    attrName.EndsWith("." + shortName, StringComparison.OrdinalIgnoreCase);

                if (!nameMatches) continue;

                var attributeText = attr.ToString();

                // If pattern is provided, check if it matches the attribute arguments
                if (!string.IsNullOrEmpty(pattern))
                {
                    if (!attributeText.Contains(pattern, StringComparison.OrdinalIgnoreCase))
                        continue;
                }

                return $"[{attributeText}]";
            }
        }

        return null;
    }
}
