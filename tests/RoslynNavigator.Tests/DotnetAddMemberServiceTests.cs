using RoslynNavigator.Services;

namespace RoslynNavigator.Tests;

public class DotnetAddMemberServiceTests
{
    // --- Shared source fixtures ---

    private const string ClassSourceNoMembers = """
        using System;

        namespace MyApp;

        public class MyClass
        {
        }
        """;

    private const string ClassSourceWithField = """
        using System;

        namespace MyApp;

        public class MyClass
        {
            private int _existingField;
        }
        """;

    private const string ClassSourceWithFieldAndProperty = """
        using System;

        namespace MyApp;

        public class MyClass
        {
            private int _existingField;

            public string Name { get; set; }
        }
        """;

    private const string ClassSourceWithAll = """
        using System;

        namespace MyApp;

        public class MyClass
        {
            private int _existingField;

            public string Name { get; set; }

            public MyClass() { }

            public void ExistingMethod() { }
        }
        """;

    private const string StructSource = """
        namespace MyApp;

        public struct MyStruct
        {
            private int _x;
        }
        """;

    private const string RecordSource = """
        namespace MyApp;

        public record MyRecord
        {
            public string Label { get; init; }
        }
        """;

    // --- AddMember: field insertion ---

    [Fact]
    public void AddMember_FieldIntoClassWithNoMembers_FieldAppearsFirstInBody()
    {
        var result = DotnetAddMemberService.AddMember(ClassSourceNoMembers, "MyClass", "field", "private int _count;");

        Assert.True(result.Success, result.Error);
        Assert.Contains("private int _count;", result.ModifiedSource);
        // Field should be inside the class body
        var classStart = result.ModifiedSource.IndexOf("public class MyClass");
        var fieldPos = result.ModifiedSource.IndexOf("private int _count;");
        Assert.True(fieldPos > classStart);
    }

    [Fact]
    public void AddMember_FieldIntoClassWithExistingField_FieldAppearsAfterLastField()
    {
        var result = DotnetAddMemberService.AddMember(ClassSourceWithField, "MyClass", "field", "private string _name;");

        Assert.True(result.Success, result.Error);
        var existingFieldPos = result.ModifiedSource.IndexOf("_existingField");
        var newFieldPos = result.ModifiedSource.IndexOf("_name;");
        Assert.True(newFieldPos > existingFieldPos, "New field should appear after existing field");
    }

    // --- AddMember: property insertion ---

    [Fact]
    public void AddMember_PropertyIntoClassWithFields_PropertyAppearsAfterFields()
    {
        var result = DotnetAddMemberService.AddMember(ClassSourceWithField, "MyClass", "property", "public int Count { get; set; }");

        Assert.True(result.Success, result.Error);
        Assert.Contains("public int Count", result.ModifiedSource);
        var fieldPos = result.ModifiedSource.IndexOf("_existingField");
        var propPos = result.ModifiedSource.IndexOf("public int Count");
        Assert.True(propPos > fieldPos, "Property should appear after fields");
    }

    [Fact]
    public void AddMember_PropertyIntoClassWithNoFieldsOrProperties_PropertyAppearsFirstInBody()
    {
        var result = DotnetAddMemberService.AddMember(ClassSourceNoMembers, "MyClass", "property", "public int Count { get; set; }");

        Assert.True(result.Success, result.Error);
        Assert.Contains("public int Count", result.ModifiedSource);
    }

    // --- AddMember: constructor insertion ---

    [Fact]
    public void AddMember_ConstructorIntoClassWithFieldsAndProperties_ConstructorAppearsAfterProperties()
    {
        var result = DotnetAddMemberService.AddMember(ClassSourceWithFieldAndProperty, "MyClass", "constructor", "public MyClass() { }");

        Assert.True(result.Success, result.Error);
        Assert.Contains("public MyClass()", result.ModifiedSource);
        var propPos = result.ModifiedSource.IndexOf("public string Name");
        var ctorPos = result.ModifiedSource.IndexOf("public MyClass()");
        Assert.True(ctorPos > propPos, "Constructor should appear after properties");
    }

    [Fact]
    public void AddMember_ConstructorIntoClassWithAllMemberKinds_ConstructorAppearsBeforeMethods()
    {
        // ClassSourceWithAll already has a constructor, add an overloaded one
        var result = DotnetAddMemberService.AddMember(ClassSourceWithAll, "MyClass", "constructor", "public MyClass(int x) { }");

        Assert.True(result.Success, result.Error);
        Assert.Contains("public MyClass(int x)", result.ModifiedSource);
        var ctorPos = result.ModifiedSource.IndexOf("public MyClass(int x)");
        var methodPos = result.ModifiedSource.IndexOf("public void ExistingMethod()");
        Assert.True(ctorPos < methodPos, "Constructor should appear before methods");
    }

    // --- AddMember: method insertion ---

    [Fact]
    public void AddMember_MethodIntoClass_MethodAppearsBeforeClosingBrace()
    {
        var result = DotnetAddMemberService.AddMember(ClassSourceWithAll, "MyClass", "method", "public void Do() { }");

        Assert.True(result.Success, result.Error);
        Assert.Contains("public void Do()", result.ModifiedSource);
        var newMethodPos = result.ModifiedSource.IndexOf("public void Do()");
        // The class closing brace should come after
        var closingBracePos = result.ModifiedSource.LastIndexOf('}');
        Assert.True(newMethodPos < closingBracePos, "Method should appear before closing brace");
    }

    [Fact]
    public void AddMember_MethodIntoEmptyClass_MethodAppearsInsideBody()
    {
        var result = DotnetAddMemberService.AddMember(ClassSourceNoMembers, "MyClass", "method", "public void Hello() { }");

        Assert.True(result.Success, result.Error);
        Assert.Contains("public void Hello()", result.ModifiedSource);
    }

    // --- AddMember: struct and record ---

    [Fact]
    public void AddMember_FieldIntoStruct_WorksCorrectly()
    {
        var result = DotnetAddMemberService.AddMember(StructSource, "MyStruct", "field", "private int _y;");

        Assert.True(result.Success, result.Error);
        Assert.Contains("private int _y;", result.ModifiedSource);
    }

    [Fact]
    public void AddMember_PropertyIntoRecord_WorksCorrectly()
    {
        var result = DotnetAddMemberService.AddMember(RecordSource, "MyRecord", "property", "public int Id { get; init; }");

        Assert.True(result.Success, result.Error);
        Assert.Contains("public int Id", result.ModifiedSource);
    }

    // --- AddMember: error cases ---

    [Fact]
    public void AddMember_TypeNotFound_ReturnsErrorWithTypeNameInMessage()
    {
        var result = DotnetAddMemberService.AddMember(ClassSourceNoMembers, "NonExistent", "method", "public void X() { }");

        Assert.False(result.Success);
        Assert.NotNull(result.Error);
        Assert.Contains("NonExistent", result.Error);
    }

    [Fact]
    public void AddMember_InvalidSyntax_ReturnsErrorWithSyntaxMessage()
    {
        var result = DotnetAddMemberService.AddMember(ClassSourceNoMembers, "MyClass", "method", "NOT VALID C# {{{{");

        Assert.False(result.Success);
        Assert.NotNull(result.Error);
        Assert.Contains("syntax", result.Error, StringComparison.OrdinalIgnoreCase);
    }

    // --- AddUsing ---

    [Fact]
    public void AddUsing_NoExistingUsings_UsingAddedAtTop()
    {
        const string source = """
            namespace MyApp;

            public class MyClass
            {
            }
            """;

        var result = DotnetAddMemberService.AddUsing(source, "System.Collections.Generic");

        Assert.True(result.Success, result.Error);
        Assert.False(result.AlreadyPresent);
        Assert.Contains("using System.Collections.Generic;", result.ModifiedSource);
        // Using should appear before the namespace
        var usingPos = result.ModifiedSource.IndexOf("using System.Collections.Generic;");
        var nsPos = result.ModifiedSource.IndexOf("namespace");
        Assert.True(usingPos < nsPos, "Using should appear before namespace declaration");
    }

    [Fact]
    public void AddUsing_UsingAlreadyPresent_ReturnsAlreadyPresentNoChange()
    {
        const string source = """
            using System;

            namespace MyApp;

            public class MyClass
            {
            }
            """;

        var result = DotnetAddMemberService.AddUsing(source, "System");

        Assert.True(result.Success);
        Assert.True(result.AlreadyPresent);
        // Source should be unchanged (or at most whitespace-normalised)
        Assert.Contains("using System;", result.ModifiedSource);
    }

    [Fact]
    public void AddUsing_NewUsingWithExistingUsings_UsingAddedAlongsideExisting()
    {
        const string source = """
            using System;
            using System.IO;

            namespace MyApp;

            public class MyClass
            {
            }
            """;

        var result = DotnetAddMemberService.AddUsing(source, "System.Collections.Generic");

        Assert.True(result.Success, result.Error);
        Assert.False(result.AlreadyPresent);
        Assert.Contains("using System.Collections.Generic;", result.ModifiedSource);
        Assert.Contains("using System;", result.ModifiedSource);
        Assert.Contains("using System.IO;", result.ModifiedSource);
    }

    // --- Indentation detection ---

    [Fact]
    public void AddMember_IndentationDetectedFromExistingMembers_InsertedContentIsIndented()
    {
        // Class using tabs for indentation
        const string tabSource = "namespace MyApp;\n\npublic class MyClass\n{\n\tprivate int _x;\n}\n";

        var result = DotnetAddMemberService.AddMember(tabSource, "MyClass", "field", "private int _y;");

        Assert.True(result.Success, result.Error);
        // The inserted field should be indented with tabs
        Assert.Contains("\tprivate int _y;", result.ModifiedSource);
    }
}
