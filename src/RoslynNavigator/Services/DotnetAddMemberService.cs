using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace RoslynNavigator.Services;

public record AddMemberResult
{
    public required bool Success { get; init; }
    public required string ModifiedSource { get; init; }
    public string? Error { get; init; }
}

public record AddUsingResult
{
    public required bool Success { get; init; }
    public required string ModifiedSource { get; init; }
    public bool AlreadyPresent { get; init; }
    public string? Error { get; init; }
}

public static class DotnetAddMemberService
{
    /// <summary>
    /// Inserts a member (field, property, constructor, or method) into the named type.
    /// </summary>
    public static AddMemberResult AddMember(string sourceText, string typeName, string memberKind, string content)
    {
        // 1. Syntax-validate the content by wrapping it in a temporary class
        var testSrc = $"class __Test__ {{\n{content}\n}}";
        var testTree = CSharpSyntaxTree.ParseText(testSrc);
        var errors = testTree.GetDiagnostics()
            .Where(d => d.Severity == DiagnosticSeverity.Error)
            .ToList();
        if (errors.Count > 0)
        {
            var msg = string.Join("; ", errors.Select(e => e.GetMessage()));
            return new AddMemberResult
            {
                Success = false,
                ModifiedSource = sourceText,
                Error = $"Syntax error in member content: {msg}"
            };
        }

        // 2. Parse the full source file
        var tree = CSharpSyntaxTree.ParseText(sourceText);
        var root = tree.GetRoot();

        // 3. Find the target type declaration
        var typeDecl = root.DescendantNodes()
            .OfType<BaseTypeDeclarationSyntax>()
            .FirstOrDefault(t => t.Identifier.Text == typeName);

        if (typeDecl == null)
        {
            return new AddMemberResult
            {
                Success = false,
                ModifiedSource = sourceText,
                Error = $"Type '{typeName}' not found in the provided source"
            };
        }

        // 4. Detect indentation from existing members
        var indentation = DetectIndentation(typeDecl);

        // 5. Apply indentation to each line of the content
        var indentedContent = ApplyIndentation(content, indentation);

        // 6. Parse the member from the indented content
        MemberDeclarationSyntax parsedMember;
        try
        {
            var memberSrc = $"class __T__ {{\n{indentedContent}\n}}";
            var memberTree = CSharpSyntaxTree.ParseText(memberSrc);
            var memberRoot = memberTree.GetRoot();
            parsedMember = memberRoot.DescendantNodes()
                .OfType<MemberDeclarationSyntax>()
                .First(n => !(n is ClassDeclarationSyntax));
        }
        catch (Exception ex)
        {
            return new AddMemberResult
            {
                Success = false,
                ModifiedSource = sourceText,
                Error = $"Failed to parse member: {ex.Message}"
            };
        }

        // 7. Determine insertion index based on member kind
        var insertIndex = ComputeInsertIndex(typeDecl, memberKind);

        // 8. Insert the member and reconstruct the source
        SyntaxNode newTypeDecl = typeDecl switch
        {
            ClassDeclarationSyntax cls => cls.WithMembers(cls.Members.Insert(insertIndex, parsedMember)),
            RecordDeclarationSyntax rec => rec.WithMembers(rec.Members.Insert(insertIndex, parsedMember)),
            StructDeclarationSyntax str => str.WithMembers(str.Members.Insert(insertIndex, parsedMember)),
            _ => throw new InvalidOperationException($"Unsupported type declaration kind: {typeDecl.Kind()}")
        };
        var newRoot = root.ReplaceNode(typeDecl, newTypeDecl);

        return new AddMemberResult
        {
            Success = true,
            ModifiedSource = newRoot.ToFullString()
        };
    }

    /// <summary>
    /// Adds a using directive to the source file. No-op if already present.
    /// </summary>
    public static AddUsingResult AddUsing(string sourceText, string namespaceName)
    {
        var tree = CSharpSyntaxTree.ParseText(sourceText);
        var root = (CompilationUnitSyntax)tree.GetRoot();

        // Check if using already present
        var alreadyPresent = root.Usings.Any(u => u.Name?.ToString() == namespaceName);
        if (alreadyPresent)
        {
            return new AddUsingResult
            {
                Success = true,
                ModifiedSource = sourceText,
                AlreadyPresent = true
            };
        }

        // Build the new using directive with proper whitespace trivia
        var nameNode = SyntaxFactory.ParseName(namespaceName)
            .WithLeadingTrivia(SyntaxFactory.Space);
        var newUsing = SyntaxFactory.UsingDirective(nameNode)
            .WithTrailingTrivia(SyntaxFactory.CarriageReturnLineFeed);

        // Insert in sorted order (alphabetical by namespace string)
        var existingUsings = root.Usings;
        int insertAt = existingUsings.Count; // default: append at end
        for (int i = 0; i < existingUsings.Count; i++)
        {
            var existing = existingUsings[i].Name?.ToString() ?? "";
            if (string.Compare(namespaceName, existing, StringComparison.Ordinal) < 0)
            {
                insertAt = i;
                break;
            }
        }

        var newUsings = existingUsings.Insert(insertAt, newUsing);
        var newRoot = root.WithUsings(newUsings);

        return new AddUsingResult
        {
            Success = true,
            ModifiedSource = newRoot.ToFullString(),
            AlreadyPresent = false
        };
    }

    // -------------------------------------------------------------------------
    // Private helpers
    // -------------------------------------------------------------------------

    private static SyntaxList<MemberDeclarationSyntax> GetMembers(BaseTypeDeclarationSyntax typeDecl) =>
        typeDecl switch
        {
            ClassDeclarationSyntax cls => cls.Members,
            RecordDeclarationSyntax rec => rec.Members,
            StructDeclarationSyntax str => str.Members,
            _ => default
        };

    private static string DetectIndentation(BaseTypeDeclarationSyntax typeDecl)
    {
        var members = GetMembers(typeDecl);
        var firstMember = members.FirstOrDefault();
        if (firstMember == null)
            return "    "; // default: 4 spaces

        var leadingTrivia = firstMember.GetLeadingTrivia().ToString();
        // Extract the last line of trivia (the actual indentation line)
        var lines = leadingTrivia.Split('\n');
        var lastLine = lines[lines.Length - 1];
        // lastLine should be whitespace only (tabs or spaces)
        if (lastLine.Length > 0 && lastLine.All(c => c == ' ' || c == '\t'))
            return lastLine;

        return "    "; // fallback
    }

    private static string ApplyIndentation(string content, string indentation)
    {
        var lines = content.Split('\n');
        var result = new List<string>();
        foreach (var line in lines)
        {
            var trimmed = line.TrimStart();
            if (trimmed.Length == 0)
                result.Add(line); // preserve blank lines as-is
            else
                result.Add(indentation + trimmed);
        }
        return string.Join('\n', result);
    }

    private static int ComputeInsertIndex(BaseTypeDeclarationSyntax typeDecl, string memberKind)
    {
        var members = GetMembers(typeDecl);

        if (members.Count == 0)
            return 0;

        switch (memberKind.ToLowerInvariant())
        {
            case "field":
            {
                // After last existing field; if none, first in body
                var lastField = members
                    .Select((m, i) => (m, i))
                    .LastOrDefault(x => x.m is FieldDeclarationSyntax);
                return lastField.m != null ? lastField.i + 1 : 0;
            }

            case "property":
            {
                // After last property; if none, after last field; if none, first
                var lastProp = members
                    .Select((m, i) => (m, i))
                    .LastOrDefault(x => x.m is PropertyDeclarationSyntax);
                if (lastProp.m != null)
                    return lastProp.i + 1;

                var lastField = members
                    .Select((m, i) => (m, i))
                    .LastOrDefault(x => x.m is FieldDeclarationSyntax);
                return lastField.m != null ? lastField.i + 1 : 0;
            }

            case "constructor":
            {
                // After last constructor; if none, after last property; if none, after last field; if none, first
                var lastCtor = members
                    .Select((m, i) => (m, i))
                    .LastOrDefault(x => x.m is ConstructorDeclarationSyntax);
                if (lastCtor.m != null)
                    return lastCtor.i + 1;

                var lastProp = members
                    .Select((m, i) => (m, i))
                    .LastOrDefault(x => x.m is PropertyDeclarationSyntax);
                if (lastProp.m != null)
                    return lastProp.i + 1;

                var lastField = members
                    .Select((m, i) => (m, i))
                    .LastOrDefault(x => x.m is FieldDeclarationSyntax);
                return lastField.m != null ? lastField.i + 1 : 0;
            }

            case "method":
            default:
            {
                // Before closing brace = at end of members list
                return members.Count;
            }
        }
    }
}
