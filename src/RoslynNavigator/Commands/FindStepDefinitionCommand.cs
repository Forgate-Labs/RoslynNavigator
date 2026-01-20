using Microsoft.CodeAnalysis.CSharp.Syntax;
using RoslynNavigator.Models;
using RoslynNavigator.Services;
using System.Text.RegularExpressions;

namespace RoslynNavigator.Commands;

public static class FindStepDefinitionCommand
{
    private static readonly string[] StepAttributeNames = { "Given", "When", "Then", "And", "But", "StepDefinition" };

    public static async Task<StepDefinitionResult> ExecuteAsync(string solutionPath, string pattern)
    {
        if (string.IsNullOrEmpty(pattern))
            throw new ArgumentException("Pattern is required");

        var solution = await WorkspaceService.GetSolutionAsync(solutionPath);
        var matches = new List<StepDefinitionInfo>();

        foreach (var project in solution.Projects)
        {
            foreach (var document in project.Documents)
            {
                if (document.FilePath == null) continue;

                var syntaxRoot = await document.GetSyntaxRootAsync();
                if (syntaxRoot == null) continue;

                foreach (var method in syntaxRoot.DescendantNodes().OfType<MethodDeclarationSyntax>())
                {
                    var stepInfo = FindStepAttribute(method, pattern, document.FilePath, solutionPath);
                    if (stepInfo != null)
                    {
                        matches.Add(stepInfo);
                    }
                }
            }
        }

        return new StepDefinitionResult
        {
            Pattern = pattern,
            Matches = matches,
            TotalCount = matches.Count
        };
    }

    private static StepDefinitionInfo? FindStepAttribute(
        MethodDeclarationSyntax method,
        string pattern,
        string filePath,
        string solutionPath)
    {
        foreach (var attrList in method.AttributeLists)
        {
            foreach (var attr in attrList.Attributes)
            {
                var attrName = attr.Name.ToString();

                // Check if it's a step attribute
                var matchedType = StepAttributeNames.FirstOrDefault(name =>
                    attrName.Equals(name, StringComparison.OrdinalIgnoreCase) ||
                    attrName.Equals(name + "Attribute", StringComparison.OrdinalIgnoreCase) ||
                    attrName.EndsWith("." + name, StringComparison.OrdinalIgnoreCase));

                if (matchedType == null) continue;

                // Extract the regex from the first argument
                var regex = ExtractRegexFromAttribute(attr);
                if (string.IsNullOrEmpty(regex)) continue;

                // Check if pattern matches (case-insensitive contains)
                if (!regex.Contains(pattern, StringComparison.OrdinalIgnoreCase))
                    continue;

                var className = RoslynAnalyzer.GetContainingClassName(method) ?? "(unknown)";
                var lineRange = RoslynAnalyzer.GetLineRange(method);

                return new StepDefinitionInfo
                {
                    Type = matchedType,
                    Regex = regex,
                    FilePath = WorkspaceService.GetRelativePath(filePath, solutionPath),
                    ClassName = className,
                    MethodName = method.Identifier.Text,
                    StartLine = lineRange[0],
                    EndLine = lineRange[1],
                    LineCount = lineRange[1] - lineRange[0] + 1,
                    Scope = DeriveScope(className)
                };
            }
        }

        return null;
    }

    private static string? ExtractRegexFromAttribute(AttributeSyntax attr)
    {
        var argumentList = attr.ArgumentList;
        if (argumentList == null || argumentList.Arguments.Count == 0)
            return null;

        var firstArg = argumentList.Arguments[0];
        var expression = firstArg.Expression;

        // Handle string literal
        if (expression is LiteralExpressionSyntax literal)
        {
            return literal.Token.ValueText;
        }

        // Handle verbatim string (@"...")
        if (expression is InterpolatedStringExpressionSyntax)
        {
            return expression.ToString();
        }

        // Fallback: return the expression as-is
        return expression.ToString().Trim('"');
    }

    private static string DeriveScope(string className)
    {
        // Remove common suffixes
        var name = className;
        foreach (var suffix in new[] { "Steps", "StepDefinitions", "StepDefs" })
        {
            if (name.EndsWith(suffix, StringComparison.OrdinalIgnoreCase))
            {
                name = name.Substring(0, name.Length - suffix.Length);
                break;
            }
        }

        // Split PascalCase into words
        var words = Regex.Replace(name, "([a-z])([A-Z])", "$1 $2");
        return words;
    }
}
