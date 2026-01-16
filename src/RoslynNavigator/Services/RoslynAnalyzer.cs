using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using RoslynNavigator.Models;

namespace RoslynNavigator.Services;

public static class RoslynAnalyzer
{
    public static string GetAccessibility(SyntaxTokenList modifiers)
    {
        if (modifiers.Any(SyntaxKind.PublicKeyword)) return "public";
        if (modifiers.Any(SyntaxKind.PrivateKeyword)) return "private";
        if (modifiers.Any(SyntaxKind.ProtectedKeyword) && modifiers.Any(SyntaxKind.InternalKeyword)) return "protected internal";
        if (modifiers.Any(SyntaxKind.ProtectedKeyword)) return "protected";
        if (modifiers.Any(SyntaxKind.InternalKeyword)) return "internal";
        return "private"; // Default for class members
    }

    public static string GetNamespace(SyntaxNode node)
    {
        var current = node.Parent;
        while (current != null)
        {
            if (current is NamespaceDeclarationSyntax namespaceDecl)
            {
                return namespaceDecl.Name.ToString();
            }
            if (current is FileScopedNamespaceDeclarationSyntax fileScopedNamespace)
            {
                return fileScopedNamespace.Name.ToString();
            }
            current = current.Parent;
        }
        return "";
    }

    public static int[] GetLineRange(SyntaxNode node)
    {
        var lineSpan = node.GetLocation().GetLineSpan();
        return new[]
        {
            lineSpan.StartLinePosition.Line + 1, // Convert to 1-based
            lineSpan.EndLinePosition.Line + 1
        };
    }

    public static int GetLine(SyntaxNode node)
    {
        return node.GetLocation().GetLineSpan().StartLinePosition.Line + 1;
    }

    public static string GetMethodSignature(MethodDeclarationSyntax method)
    {
        var accessibility = GetAccessibility(method.Modifiers);
        var modifiers = new List<string> { accessibility };

        if (method.Modifiers.Any(SyntaxKind.StaticKeyword)) modifiers.Add("static");
        if (method.Modifiers.Any(SyntaxKind.AsyncKeyword)) modifiers.Add("async");
        if (method.Modifiers.Any(SyntaxKind.VirtualKeyword)) modifiers.Add("virtual");
        if (method.Modifiers.Any(SyntaxKind.OverrideKeyword)) modifiers.Add("override");
        if (method.Modifiers.Any(SyntaxKind.AbstractKeyword)) modifiers.Add("abstract");

        var returnType = method.ReturnType.ToString();
        var methodName = method.Identifier.Text;
        var typeParams = method.TypeParameterList?.ToString() ?? "";
        var parameters = method.ParameterList.ToString();

        return $"{string.Join(" ", modifiers)} {returnType} {methodName}{typeParams}{parameters}";
    }

    public static string GetConstructorSignature(ConstructorDeclarationSyntax ctor)
    {
        var accessibility = GetAccessibility(ctor.Modifiers);
        var modifiers = new List<string> { accessibility };

        if (ctor.Modifiers.Any(SyntaxKind.StaticKeyword)) modifiers.Add("static");

        var ctorName = ctor.Identifier.Text;
        var parameters = ctor.ParameterList.ToString();

        return $"{string.Join(" ", modifiers)} {ctorName}{parameters}";
    }

    public static List<ParameterInfo> GetParameters(ParameterListSyntax parameterList)
    {
        return parameterList.Parameters.Select(p => new ParameterInfo
        {
            Name = p.Identifier.Text,
            Type = p.Type?.ToString() ?? "var"
        }).ToList();
    }

    public static string? GetContainingMethodName(SyntaxNode node)
    {
        var current = node.Parent;
        while (current != null)
        {
            if (current is MethodDeclarationSyntax method)
            {
                return method.Identifier.Text;
            }
            if (current is ConstructorDeclarationSyntax ctor)
            {
                return ctor.Identifier.Text;
            }
            if (current is PropertyDeclarationSyntax property)
            {
                return property.Identifier.Text;
            }
            current = current.Parent;
        }
        return null;
    }

    public static string? GetContainingClassName(SyntaxNode node)
    {
        var current = node.Parent;
        while (current != null)
        {
            if (current is ClassDeclarationSyntax classDecl)
            {
                return classDecl.Identifier.Text;
            }
            if (current is StructDeclarationSyntax structDecl)
            {
                return structDecl.Identifier.Text;
            }
            if (current is RecordDeclarationSyntax recordDecl)
            {
                return recordDecl.Identifier.Text;
            }
            current = current.Parent;
        }
        return null;
    }

    public static async Task<ClassStructure?> AnalyzeClassAsync(Document document, string className, string solutionPath)
    {
        var syntaxRoot = await document.GetSyntaxRootAsync();
        if (syntaxRoot == null) return null;

        var classNode = syntaxRoot.DescendantNodes()
            .OfType<TypeDeclarationSyntax>()
            .FirstOrDefault(c => c.Identifier.Text == className);

        if (classNode == null) return null;

        var members = new List<MemberInfo>();

        // Fields
        foreach (var field in classNode.Members.OfType<FieldDeclarationSyntax>())
        {
            foreach (var variable in field.Declaration.Variables)
            {
                members.Add(new MemberInfo
                {
                    Kind = "field",
                    Name = variable.Identifier.Text,
                    Type = field.Declaration.Type.ToString(),
                    Line = GetLine(field),
                    Accessibility = GetAccessibility(field.Modifiers),
                    IsReadonly = field.Modifiers.Any(SyntaxKind.ReadOnlyKeyword),
                    IsStatic = field.Modifiers.Any(SyntaxKind.StaticKeyword)
                });
            }
        }

        // Properties
        foreach (var property in classNode.Members.OfType<PropertyDeclarationSyntax>())
        {
            members.Add(new MemberInfo
            {
                Kind = "property",
                Name = property.Identifier.Text,
                Type = property.Type.ToString(),
                Line = GetLine(property),
                Accessibility = GetAccessibility(property.Modifiers),
                HasGetter = property.AccessorList?.Accessors.Any(a => a.Kind() == SyntaxKind.GetAccessorDeclaration) ?? property.ExpressionBody != null,
                HasSetter = property.AccessorList?.Accessors.Any(a => a.Kind() == SyntaxKind.SetAccessorDeclaration || a.Kind() == SyntaxKind.InitAccessorDeclaration) ?? false,
                IsStatic = property.Modifiers.Any(SyntaxKind.StaticKeyword)
            });
        }

        // Constructors
        foreach (var ctor in classNode.Members.OfType<ConstructorDeclarationSyntax>())
        {
            members.Add(new MemberInfo
            {
                Kind = "constructor",
                Name = ctor.Identifier.Text,
                LineRange = GetLineRange(ctor),
                Signature = GetConstructorSignature(ctor),
                Accessibility = GetAccessibility(ctor.Modifiers),
                IsStatic = ctor.Modifiers.Any(SyntaxKind.StaticKeyword),
                Parameters = GetParameters(ctor.ParameterList)
            });
        }

        // Methods
        foreach (var method in classNode.Members.OfType<MethodDeclarationSyntax>())
        {
            members.Add(new MemberInfo
            {
                Kind = "method",
                Name = method.Identifier.Text,
                LineRange = GetLineRange(method),
                Signature = GetMethodSignature(method),
                Accessibility = GetAccessibility(method.Modifiers),
                IsAsync = method.Modifiers.Any(SyntaxKind.AsyncKeyword),
                IsStatic = method.Modifiers.Any(SyntaxKind.StaticKeyword),
                ReturnType = method.ReturnType.ToString(),
                Parameters = GetParameters(method.ParameterList)
            });
        }

        return new ClassStructure
        {
            ClassName = className,
            Namespace = GetNamespace(classNode),
            LineRange = GetLineRange(classNode),
            FilePath = WorkspaceService.GetRelativePath(document.FilePath!, solutionPath),
            Members = members
        };
    }
}
