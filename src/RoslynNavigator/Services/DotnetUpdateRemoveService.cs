using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace RoslynNavigator.Services;

public record UpdateMemberResult
{
    public required bool Success { get; init; }
    public required string ModifiedSource { get; init; }
    public string? Error { get; init; }
}

public record RemoveMemberResult
{
    public required bool Success { get; init; }
    public required string ModifiedSource { get; init; }
    public string? Error { get; init; }
}

public static class DotnetUpdateRemoveService
{
    /// <summary>
    /// Replaces an existing property or field declaration (identified by name) with newContent.
    /// memberKind: "property" | "field"
    /// </summary>
    public static UpdateMemberResult UpdateMember(
        string sourceText,
        string typeName,
        string memberKind,
        string memberName,
        string newContent)
    {
        // 1. Syntax-validate newContent
        var testSrc = $"class __Test__ {{\n{newContent}\n}}";
        var testTree = CSharpSyntaxTree.ParseText(testSrc);
        var errors = testTree.GetDiagnostics()
            .Where(d => d.Severity == DiagnosticSeverity.Error)
            .ToList();
        if (errors.Count > 0)
        {
            var msg = string.Join("; ", errors.Select(e => e.GetMessage()));
            return new UpdateMemberResult
            {
                Success = false,
                ModifiedSource = sourceText,
                Error = $"Syntax error in member content: {msg}"
            };
        }

        // 2. Parse the full source
        var tree = CSharpSyntaxTree.ParseText(sourceText);
        var root = tree.GetRoot();

        // 3. Find the target type declaration
        var typeDecl = root.DescendantNodes()
            .OfType<BaseTypeDeclarationSyntax>()
            .FirstOrDefault(t => t.Identifier.Text == typeName);

        if (typeDecl == null)
        {
            return new UpdateMemberResult
            {
                Success = false,
                ModifiedSource = sourceText,
                Error = $"Type '{typeName}' not found in the provided source"
            };
        }

        // 4. Locate the member to replace
        var members = GetMembers(typeDecl);
        MemberDeclarationSyntax? foundMember = memberKind.ToLowerInvariant() switch
        {
            "property" => members
                .OfType<PropertyDeclarationSyntax>()
                .FirstOrDefault(p => p.Identifier.Text == memberName),
            "field" => members
                .OfType<FieldDeclarationSyntax>()
                .FirstOrDefault(f => FieldNameMatches(f, memberName)),
            _ => null
        };

        if (foundMember == null)
        {
            return new UpdateMemberResult
            {
                Success = false,
                ModifiedSource = sourceText,
                Error = $"Member '{memberName}' of kind '{memberKind}' not found in type '{typeName}'"
            };
        }

        // 5. Detect indentation and parse the replacement node
        var indentation = DetectIndentation(typeDecl);
        var indentedContent = ApplyIndentation(newContent, indentation);

        MemberDeclarationSyntax parsedReplacement;
        try
        {
            var memberSrc = $"class __T__ {{\n{indentedContent}\n}}";
            var memberTree = CSharpSyntaxTree.ParseText(memberSrc);
            var memberRoot = memberTree.GetRoot();
            parsedReplacement = memberRoot.DescendantNodes()
                .OfType<MemberDeclarationSyntax>()
                .First(n => !(n is ClassDeclarationSyntax));
        }
        catch (Exception ex)
        {
            return new UpdateMemberResult
            {
                Success = false,
                ModifiedSource = sourceText,
                Error = $"Failed to parse replacement member: {ex.Message}"
            };
        }

        // 6. Replace old node with parsed replacement
        var newRoot = root.ReplaceNode(foundMember, parsedReplacement);

        return new UpdateMemberResult
        {
            Success = true,
            ModifiedSource = newRoot.ToFullString()
        };
    }

    /// <summary>
    /// Removes a method, property, or field declaration identified by name from the type.
    /// memberKind: "method" | "property" | "field"
    /// </summary>
    public static RemoveMemberResult RemoveMember(
        string sourceText,
        string typeName,
        string memberKind,
        string memberName)
    {
        // 1. Parse the full source
        var tree = CSharpSyntaxTree.ParseText(sourceText);
        var root = tree.GetRoot();

        // 2. Find the target type declaration
        var typeDecl = root.DescendantNodes()
            .OfType<BaseTypeDeclarationSyntax>()
            .FirstOrDefault(t => t.Identifier.Text == typeName);

        if (typeDecl == null)
        {
            return new RemoveMemberResult
            {
                Success = false,
                ModifiedSource = sourceText,
                Error = $"Type '{typeName}' not found in the provided source"
            };
        }

        // 3. Locate the member to remove
        var members = GetMembers(typeDecl);
        MemberDeclarationSyntax? foundMember = memberKind.ToLowerInvariant() switch
        {
            "method" => members
                .OfType<MethodDeclarationSyntax>()
                .FirstOrDefault(m => m.Identifier.Text == memberName),
            "property" => members
                .OfType<PropertyDeclarationSyntax>()
                .FirstOrDefault(p => p.Identifier.Text == memberName),
            "field" => members
                .OfType<FieldDeclarationSyntax>()
                .FirstOrDefault(f => FieldNameMatches(f, memberName)),
            _ => null
        };

        if (foundMember == null)
        {
            return new RemoveMemberResult
            {
                Success = false,
                ModifiedSource = sourceText,
                Error = $"Member '{memberName}' of kind '{memberKind}' not found in type '{typeName}'"
            };
        }

        // 4. Remove the member via switch on concrete type
        SyntaxNode newTypeDecl = typeDecl switch
        {
            ClassDeclarationSyntax cls =>
                cls.WithMembers(cls.Members.Remove(foundMember)),
            RecordDeclarationSyntax rec =>
                rec.WithMembers(rec.Members.Remove(foundMember)),
            StructDeclarationSyntax str =>
                str.WithMembers(str.Members.Remove(foundMember)),
            _ => throw new InvalidOperationException($"Unsupported type declaration kind: {typeDecl.Kind()}")
        };

        var newRoot = root.ReplaceNode(typeDecl, newTypeDecl);

        return new RemoveMemberResult
        {
            Success = true,
            ModifiedSource = newRoot.ToFullString()
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

    /// <summary>
    /// Matches a field declaration against a memberName using underscore-tolerant logic.
    /// "_count" matches memberName "_count" (exact) or "count" (normalized).
    /// "count" matches memberName "count" (exact) or "_count" (exact).
    /// </summary>
    private static bool FieldNameMatches(FieldDeclarationSyntax field, string memberName)
    {
        var normalized = memberName.TrimStart('_');
        return field.Declaration.Variables.Any(v =>
            v.Identifier.Text == memberName ||                    // exact match ("_count")
            v.Identifier.Text.TrimStart('_') == normalized);     // normalized match ("count")
    }

    private static string DetectIndentation(BaseTypeDeclarationSyntax typeDecl)
    {
        var members = GetMembers(typeDecl);
        var firstMember = members.FirstOrDefault();
        if (firstMember == null)
            return "    "; // default: 4 spaces

        var leadingTrivia = firstMember.GetLeadingTrivia().ToString();
        var lines = leadingTrivia.Split('\n');
        var lastLine = lines[lines.Length - 1];
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
}
