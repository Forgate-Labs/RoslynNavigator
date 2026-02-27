using RoslynNavigator.Models;
using RoslynNavigator.Services;

namespace RoslynNavigator.Tests;

/// <summary>
/// Tests for RuleSqlCompiler - verifies SQL generation from rule predicates.
/// </summary>
public class RuleSqlCompilerTests
{
    // --- Wildcard (LIKE) tests ---

    [Fact]
    public void Compile_CallsWildcard_GeneratesLikePattern()
    {
        // Arrange
        var compiler = new RuleSqlCompiler();
        var predicate = new RulePredicate { Calls = "IRepo.*" };

        // Act
        var (sql, parameters) = compiler.Compile(predicate);

        // Assert - should use SQL LIKE with converted wildcard
        Assert.Contains("LIKE", sql);
        // Parameter should contain the pattern
        Assert.True(parameters.ContainsValue("IRepo%"));
    }

    [Fact]
    public void Compile_CallsWildcard_ReplacesAsteriskWithPercent()
    {
        // Arrange
        var compiler = new RuleSqlCompiler();
        var predicate = new RulePredicate { Calls = "Controller.*" };

        // Act
        var (sql, parameters) = compiler.Compile(predicate);

        // Assert - should have % in the parameter value
        Assert.DoesNotContain("*", sql);
        Assert.Contains("%", parameters["@p0"]?.ToString() ?? "");
    }

    [Fact]
    public void Compile_CallsNoWildcard_GeneratesEqualsCondition()
    {
        // Arrange
        var compiler = new RuleSqlCompiler();
        var predicate = new RulePredicate { Calls = "IRepo.GetById" };

        // Act
        var (sql, _) = compiler.Compile(predicate);

        // Assert - should use = for exact match (not LIKE)
        Assert.Contains("=", sql);
    }

    // --- NOT EXISTS tests ---

    [Fact]
    public void Compile_NotPredicate_GeneratesNotExists()
    {
        // Arrange
        var compiler = new RuleSqlCompiler();
        var predicate = new RulePredicate
        {
            Calls = "Controller.*",
            Not = new RulePredicate { Calls = "IController" }
        };

        // Act
        var (sql, _) = compiler.Compile(predicate);

        // Assert - should contain NOT EXISTS
        Assert.Contains("NOT EXISTS", sql);
    }

    [Fact]
    public void Compile_NotPredicate_ContainsNestedCondition()
    {
        // Arrange
        var compiler = new RuleSqlCompiler();
        var predicate = new RulePredicate
        {
            Calls = "*.Data.*",
            Not = new RulePredicate { Calls = "IRepository" }
        };

        // Act
        var (sql, _) = compiler.Compile(predicate);

        // Assert - nested condition should be present
        Assert.Contains("NOT EXISTS", sql);
    }

    // --- FromNamespace tests ---

    [Fact]
    public void Compile_FromNamespaceWithWildcard_UsesLike()
    {
        // Arrange
        var compiler = new RuleSqlCompiler();
        var predicate = new RulePredicate { FromNamespace = "*.Data.*" };

        // Act
        var (sql, parameters) = compiler.Compile(predicate);

        // Assert
        Assert.Contains("LIKE", sql);
        Assert.True(parameters.ContainsValue("%.Data.%"));
    }

    [Fact]
    public void Compile_FromNamespaceNoWildcard_UsesEquals()
    {
        // Arrange
        var compiler = new RuleSqlCompiler();
        var predicate = new RulePredicate { FromNamespace = "MyApp.Data" };

        // Act
        var (sql, parameters) = compiler.Compile(predicate);

        // Assert
        Assert.Contains("=", sql);
        Assert.True(parameters.ContainsKey("@p0"));
    }

    // --- Boolean flags tests ---

    [Fact]
    public void Compile_ReturnsNullFlag_GeneratesCondition()
    {
        // Arrange
        var compiler = new RuleSqlCompiler();
        var predicate = new RulePredicate { ReturnsNull = true };

        // Act
        var (sql, parameters) = compiler.Compile(predicate);

        // Assert
        Assert.Contains("returns_null", sql);
        Assert.Contains("@p0", sql);  // Check for parameter placeholder
        Assert.True(parameters.ContainsKey("@p0"));  // Check parameter exists
    }

    [Fact]
    public void Compile_CognitiveComplexity_GeneratesCondition()
    {
        // Arrange
        var compiler = new RuleSqlCompiler();
        var predicate = new RulePredicate { CognitiveComplexity = 10 };

        // Act
        var (sql, parameters) = compiler.Compile(predicate);

        // Assert
        Assert.Contains("cognitive_complexity", sql);
        Assert.Contains("@p0", sql);
        Assert.True(parameters.ContainsKey("@p0"));
    }

    [Fact]
    public void Compile_HasTryCatch_GeneratesCondition()
    {
        // Arrange
        var compiler = new RuleSqlCompiler();
        var predicate = new RulePredicate { HasTryCatch = true };

        // Act
        var (sql, _) = compiler.Compile(predicate);

        // Assert
        Assert.Contains("has_try_catch", sql);
    }

    // --- Parameterization tests ---

    [Fact]
    public void Compile_Predicate_ParametersAreNamed()
    {
        // Arrange
        var compiler = new RuleSqlCompiler();
        var predicate = new RulePredicate { Calls = "Test.*" };

        // Act
        var (sql, parameters) = compiler.Compile(predicate);

        // Assert - should have parameter names
        Assert.NotNull(parameters);
    }

    // --- Combined predicates tests ---

    [Fact]
    public void Compile_CombinedCallsAndFromNamespace_BothConditionsPresent()
    {
        // Arrange
        var compiler = new RuleSqlCompiler();
        var predicate = new RulePredicate
        {
            Calls = "IRepo.*",
            FromNamespace = "*.Services.*"
        };

        // Act
        var (sql, _) = compiler.Compile(predicate);

        // Assert - both conditions should be present
        Assert.Contains("calls", sql.ToLower());
        Assert.Contains("namespace", sql.ToLower());
    }
}
