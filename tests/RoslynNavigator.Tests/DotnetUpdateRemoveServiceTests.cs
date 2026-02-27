using RoslynNavigator.Services;

namespace RoslynNavigator.Tests;

public class DotnetUpdateRemoveServiceTests
{
    // --- Shared source fixtures ---

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

    private const string ClassSourceWithProperty = """
        using System;

        namespace MyApp;

        public class MyClass
        {
            public string Name { get; set; }
        }
        """;

    private const string ClassSourceWithField = """
        using System;

        namespace MyApp;

        public class MyClass
        {
            private int _count;
        }
        """;

    private const string StructSourceWithField = """
        namespace MyApp;

        public struct MyStruct
        {
            private int _x;
        }
        """;

    private const string RecordSourceWithProperty = """
        namespace MyApp;

        public record MyRecord
        {
            public int Id { get; init; }
        }
        """;

    // -------------------------------------------------------------------------
    // UpdateMember tests
    // -------------------------------------------------------------------------

    [Fact]
    public void UpdateMember_ReplaceProperty_OldGetterSetterReplaced()
    {
        var result = DotnetUpdateRemoveService.UpdateMember(
            ClassSourceWithProperty, "MyClass", "property", "Name",
            "public string Name { get; init; }");

        Assert.True(result.Success, result.Error);
        Assert.Contains("get; init;", result.ModifiedSource);
        Assert.DoesNotContain("get; set;", result.ModifiedSource);
    }

    [Fact]
    public void UpdateMember_ReplaceField_ByExactName_FieldReplaced()
    {
        var result = DotnetUpdateRemoveService.UpdateMember(
            ClassSourceWithField, "MyClass", "field", "_count",
            "private long _count;");

        Assert.True(result.Success, result.Error);
        Assert.Contains("private long _count;", result.ModifiedSource);
        Assert.DoesNotContain("private int _count;", result.ModifiedSource);
    }

    [Fact]
    public void UpdateMember_ReplaceField_ByNormalizedName_UnderscoreTolerant()
    {
        // Caller provides "count" (no underscore), but field is "_count"
        var result = DotnetUpdateRemoveService.UpdateMember(
            ClassSourceWithField, "MyClass", "field", "count",
            "private long _count;");

        Assert.True(result.Success, result.Error);
        Assert.Contains("private long _count;", result.ModifiedSource);
        Assert.DoesNotContain("private int _count;", result.ModifiedSource);
    }

    [Fact]
    public void UpdateMember_MemberNotFound_ReturnsErrorWithNotFoundMessage()
    {
        var result = DotnetUpdateRemoveService.UpdateMember(
            ClassSourceWithProperty, "MyClass", "property", "Missing",
            "public string Missing { get; set; }");

        Assert.False(result.Success);
        Assert.NotNull(result.Error);
        Assert.Contains("not found", result.Error, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void UpdateMember_InvalidNewContent_ReturnsErrorWithSyntaxMessage()
    {
        var result = DotnetUpdateRemoveService.UpdateMember(
            ClassSourceWithProperty, "MyClass", "property", "Name",
            "NOT VALID C# {{{{");

        Assert.False(result.Success);
        Assert.NotNull(result.Error);
        Assert.Contains("syntax", result.Error, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void UpdateMember_ReplacePropertyInStruct_WorksCorrectly()
    {
        const string structSourceWithProp = """
            namespace MyApp;

            public struct MyStruct
            {
                public int Value { get; set; }
            }
            """;

        var result = DotnetUpdateRemoveService.UpdateMember(
            structSourceWithProp, "MyStruct", "property", "Value",
            "public int Value { get; init; }");

        Assert.True(result.Success, result.Error);
        Assert.Contains("get; init;", result.ModifiedSource);
        Assert.DoesNotContain("get; set;", result.ModifiedSource);
    }

    [Fact]
    public void UpdateMember_ReplacePropertyInRecord_WorksCorrectly()
    {
        var result = DotnetUpdateRemoveService.UpdateMember(
            RecordSourceWithProperty, "MyRecord", "property", "Id",
            "public int Id { get; init; }");

        Assert.True(result.Success, result.Error);
        Assert.Contains("public int Id { get; init; }", result.ModifiedSource);
    }

    // -------------------------------------------------------------------------
    // RemoveMember tests
    // -------------------------------------------------------------------------

    [Fact]
    public void RemoveMember_RemoveMethod_MethodGoneRestIntact()
    {
        var result = DotnetUpdateRemoveService.RemoveMember(
            ClassSourceWithAll, "MyClass", "method", "ExistingMethod");

        Assert.True(result.Success, result.Error);
        Assert.DoesNotContain("ExistingMethod", result.ModifiedSource);
        // Other members should still be present
        Assert.Contains("_existingField", result.ModifiedSource);
        Assert.Contains("public string Name", result.ModifiedSource);
    }

    [Fact]
    public void RemoveMember_RemoveProperty_PropertyGoneRestIntact()
    {
        var result = DotnetUpdateRemoveService.RemoveMember(
            ClassSourceWithAll, "MyClass", "property", "Name");

        Assert.True(result.Success, result.Error);
        Assert.DoesNotContain("public string Name", result.ModifiedSource);
        Assert.Contains("_existingField", result.ModifiedSource);
        Assert.Contains("ExistingMethod", result.ModifiedSource);
    }

    [Fact]
    public void RemoveMember_RemoveField_ByExactName_FieldGone()
    {
        var result = DotnetUpdateRemoveService.RemoveMember(
            ClassSourceWithAll, "MyClass", "field", "_existingField");

        Assert.True(result.Success, result.Error);
        Assert.DoesNotContain("_existingField", result.ModifiedSource);
        Assert.Contains("public string Name", result.ModifiedSource);
        Assert.Contains("ExistingMethod", result.ModifiedSource);
    }

    [Fact]
    public void RemoveMember_RemoveField_ByNormalizedName_UnderscoreTolerant()
    {
        // Caller provides "existingField" (no underscore), field is "_existingField"
        var result = DotnetUpdateRemoveService.RemoveMember(
            ClassSourceWithAll, "MyClass", "field", "existingField");

        Assert.True(result.Success, result.Error);
        Assert.DoesNotContain("_existingField", result.ModifiedSource);
    }

    [Fact]
    public void RemoveMember_MemberNotFound_ReturnsErrorWithNotFoundMessage()
    {
        var result = DotnetUpdateRemoveService.RemoveMember(
            ClassSourceWithAll, "MyClass", "method", "Nonexistent");

        Assert.False(result.Success);
        Assert.NotNull(result.Error);
        Assert.Contains("not found", result.Error, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void RemoveMember_RemoveFieldFromStruct_WorksCorrectly()
    {
        var result = DotnetUpdateRemoveService.RemoveMember(
            StructSourceWithField, "MyStruct", "field", "_x");

        Assert.True(result.Success, result.Error);
        Assert.DoesNotContain("_x", result.ModifiedSource);
    }
}
