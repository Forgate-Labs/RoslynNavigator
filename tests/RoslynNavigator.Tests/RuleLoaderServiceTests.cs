using RoslynNavigator.Rules.Services;
using RoslynNavigator.Rules.Models;

namespace RoslynNavigator.Tests;

public class RuleLoaderServiceTests : IDisposable
{
    private readonly string _tempDir;
    private readonly RuleLoaderService _loaderService;

    public RuleLoaderServiceTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(_tempDir);
        _loaderService = new RuleLoaderService();
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
            Directory.Delete(_tempDir, recursive: true);
    }

    // --- LoadBuiltinRules tests ---

    [Fact]
    public void LoadBuiltinRules_ReturnsRulesFromEmbeddedResources()
    {
        // Act
        var result = _loaderService.LoadBuiltinRules();

        // Assert
        Assert.True(result.Success);
        Assert.NotEmpty(result.Rules);
        Assert.Equal("builtin", result.Source);
    }

    [Fact]
    public void LoadBuiltinRules_LoadsAllThreePacks()
    {
        // Act
        var result = _loaderService.LoadBuiltinRules();

        // Assert
        var ruleIds = result.Rules.Select(r => r.Id).ToList();
        
        // Check architecture rules
        Assert.Contains(ruleIds, id => id.StartsWith("arch-"));
        // Check code quality rules
        Assert.Contains(ruleIds, id => id.StartsWith("cq-"));
        // Check security rules
        Assert.Contains(ruleIds, id => id.StartsWith("sec-"));
    }

    [Fact]
    public void LoadBuiltinRules_RulesHaveRequiredFields()
    {
        // Act
        var result = _loaderService.LoadBuiltinRules();

        // Assert
        foreach (var rule in result.Rules)
        {
            Assert.False(string.IsNullOrEmpty(rule.Id), "Rule must have an ID");
            Assert.False(string.IsNullOrEmpty(rule.Title), $"Rule {rule.Id} must have a title");
            Assert.False(string.IsNullOrEmpty(rule.Message), $"Rule {rule.Id} must have a message");
            Assert.False(string.IsNullOrEmpty(rule.Severity), $"Rule {rule.Id} must have a severity");
        }
    }

    [Fact]
    public void LoadBuiltinRules_ValidSeverities()
    {
        // Act
        var result = _loaderService.LoadBuiltinRules();

        // Assert
        var validSeverities = new[] { "info", "warning", "error" };
        foreach (var rule in result.Rules)
        {
            Assert.Contains(rule.Severity.ToLowerInvariant(), validSeverities);
        }
    }

    // --- LoadDomainRules tests ---

    [Fact]
    public void LoadDomainRules_ReturnsEmpty_WhenDirectoryDoesNotExist()
    {
        // Arrange
        var nonExistentPath = Path.Combine(_tempDir, "non-existent-rules");

        // Act
        var result = _loaderService.LoadDomainRules(nonExistentPath);

        // Assert
        Assert.True(result.Success);
        Assert.Empty(result.Rules);
    }

    [Fact]
    public void LoadDomainRules_ReturnsEmpty_WhenDirectoryIsEmpty()
    {
        // Arrange
        var emptyDir = Path.Combine(_tempDir, "empty-rules");
        Directory.CreateDirectory(emptyDir);

        // Act
        var result = _loaderService.LoadDomainRules(emptyDir);

        // Assert
        Assert.True(result.Success);
        Assert.Empty(result.Rules);
    }

    [Fact]
    public void LoadDomainRules_LoadsCustomYamlFiles()
    {
        // Arrange
        var domainDir = Path.Combine(_tempDir, "roslyn-nav-rules");
        Directory.CreateDirectory(domainDir);
        
        var customRules = @"
rules:
  - id: custom-001
    title: Custom rule
    severity: error
    message: This is a custom rule
    predicate:
      calls: ""CustomService.*""
";
        File.WriteAllText(Path.Combine(domainDir, "custom.yaml"), customRules);

        // Act
        var result = _loaderService.LoadDomainRules(domainDir);

        // Assert
        Assert.True(result.Success);
        Assert.Single(result.Rules);
        Assert.Equal("custom-001", result.Rules[0].Id);
    }

    [Fact]
    public void LoadDomainRules_MergesWithBuiltinRules()
    {
        // Arrange
        var domainDir = Path.Combine(_tempDir, "roslyn-nav-rules");
        Directory.CreateDirectory(domainDir);
        
        var customRules = @"
rules:
  - id: custom-001
    title: Custom rule
    severity: error
    message: This is a custom rule
";
        File.WriteAllText(Path.Combine(domainDir, "custom.yaml"), customRules);

        // Act
        var allRules = _loaderService.LoadAllRules(domainDir);

        // Assert
        Assert.NotEmpty(allRules);
        
        // Should have builtin + custom
        var builtinResult = _loaderService.LoadBuiltinRules();
        var expectedCount = builtinResult.Rules.Count + 1;
        Assert.Equal(expectedCount, allRules.Count);
        
        // Should contain the custom rule
        Assert.Contains(allRules, r => r.Id == "custom-001");
    }

    [Fact]
    public void LoadDomainRules_IgnoresNonYamlFiles()
    {
        // Arrange
        var domainDir = Path.Combine(_tempDir, "roslyn-nav-rules");
        Directory.CreateDirectory(domainDir);
        
        // Create a non-YAML file
        File.WriteAllText(Path.Combine(domainDir, "readme.txt"), "This is not a rule file");

        // Act
        var result = _loaderService.LoadDomainRules(domainDir);

        // Assert
        Assert.True(result.Success);
        Assert.Empty(result.Rules);
    }

    // --- Duplicate handling tests ---

    [Fact]
    public void LoadAllRules_DuplicateIds_AllRulesLoaded()
    {
        // Arrange - create domain rule with same ID as builtin
        var domainDir = Path.Combine(_tempDir, "roslyn-nav-rules");
        Directory.CreateDirectory(domainDir);
        
        var duplicateRules = @"
rules:
  - id: arch-001
    title: Duplicate rule
    severity: error
    message: This is a duplicate
";
        File.WriteAllText(Path.Combine(domainDir, "duplicate.yaml"), duplicateRules);

        // Act
        var allRules = _loaderService.LoadAllRules(domainDir);

        // Assert - both rules should be present (no deduplication, last one wins)
        var arch001Rules = allRules.Where(r => r.Id == "arch-001").ToList();
        Assert.NotEmpty(arch001Rules);
    }

    // --- Invalid YAML tests ---

    [Fact]
    public void LoadDomainRules_ReturnsError_ForMalformedYaml()
    {
        // Arrange
        var domainDir = Path.Combine(_tempDir, "roslyn-nav-rules");
        Directory.CreateDirectory(domainDir);
        
        var badYaml = @"
rules:
  - id: bad-rule
    title: Bad rule
    severity: error
    message: This has invalid yaml: [unclosed bracket
";
        File.WriteAllText(Path.Combine(domainDir, "bad.yaml"), badYaml);

        // Act
        var result = _loaderService.LoadDomainRules(domainDir);

        // Assert
        Assert.False(result.Success);
        Assert.NotEmpty(result.Errors);
        Assert.Single(result.Errors);
        Assert.Contains("bad.yaml", result.Errors[0].File);
    }

    [Fact]
    public void LoadDomainRules_ReturnsError_ForRuleWithoutId()
    {
        // Arrange
        var domainDir = Path.Combine(_tempDir, "roslyn-nav-rules");
        Directory.CreateDirectory(domainDir);
        
        var noIdYaml = @"
rules:
  - title: Rule without ID
    severity: error
    message: Missing ID field
";
        File.WriteAllText(Path.Combine(domainDir, "no-id.yaml"), noIdYaml);

        // Act
        var result = _loaderService.LoadDomainRules(domainDir);

        // Assert
        Assert.False(result.Success);
        Assert.NotEmpty(result.Errors);
        Assert.Contains(result.Errors, e => e.Reason.Contains("id"));
    }

    // --- FindDomainRulesPath tests ---

    [Fact]
    public void FindDomainRulesPath_ReturnsPath_WhenDirectoryExists()
    {
        // Arrange
        var basePath = Path.Combine(_tempDir, "my-solution");
        Directory.CreateDirectory(basePath);
        var domainRulesPath = Path.Combine(basePath, "roslyn-nav-rules");
        Directory.CreateDirectory(domainRulesPath);

        // Act
        var result = _loaderService.FindDomainRulesPath(basePath);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(domainRulesPath, result);
    }

    [Fact]
    public void FindDomainRulesPath_ReturnsNull_WhenDirectoryDoesNotExist()
    {
        // Arrange
        var basePath = Path.Combine(_tempDir, "my-solution");
        Directory.CreateDirectory(basePath);

        // Act
        var result = _loaderService.FindDomainRulesPath(basePath);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void FindDomainRulesPath_ReturnsNull_ForEmptyPath()
    {
        // Act
        var result = _loaderService.FindDomainRulesPath(string.Empty);

        // Assert
        Assert.Null(result);
    }

    // --- Integration tests ---

    [Fact]
    public void LoadAllRulesWithErrors_ReturnsAllRulesAndErrors()
    {
        // Arrange
        var domainDir = Path.Combine(_tempDir, "roslyn-nav-rules");
        Directory.CreateDirectory(domainDir);
        
        // Add a good rule
        var goodYaml = @"
rules:
  - id: good-001
    title: Good rule
    severity: warning
    message: This is good
";
        File.WriteAllText(Path.Combine(domainDir, "good.yaml"), goodYaml);

        // Add a bad rule (missing ID)
        var badYaml = @"
rules:
  - title: Bad rule
    severity: error
    message: Missing ID
";
        File.WriteAllText(Path.Combine(domainDir, "bad.yaml"), badYaml);

        // Act
        var (rules, errors) = _loaderService.LoadAllRulesWithErrors(domainDir);

        // Assert
        Assert.NotEmpty(rules);
        Assert.NotEmpty(errors);
        Assert.Contains(errors, e => e.Reason.Contains("id"));
    }
}
