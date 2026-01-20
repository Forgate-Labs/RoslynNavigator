# CLAUDE.md - Roslyn Navigator Integration

This file teaches Claude Code how to use the `roslyn-nav` tool for efficient C# code navigation.

## What is Roslyn Navigator?

A .NET global tool that provides semantic C# code analysis using Roslyn. Instead of reading entire files, use targeted commands to extract only the information you need, reducing token usage by 85%+.

## Installation

```bash
dotnet tool install --global RoslynNavigator
```

## Available Commands

### 1. `list-class` - Get Class Overview

Use this FIRST when exploring a class. Returns all members (fields, properties, methods) with line ranges.

```bash
roslyn-nav list-class --solution <path.sln> --file <path.cs> --class <ClassName>
```

**When to use:** Before reading a file, get the class structure to identify which methods to examine.

### 2. `find-symbol` - Locate Any Symbol

Find where a class, method, or property is defined in the solution.

```bash
roslyn-nav find-symbol --solution <path.sln> --name <SymbolName> --kind <class|method|property>
```

**When to use:** When you need to find where something is defined but don't know the file.

### 3. `get-method` - Extract Method Source Code

Get the complete source code of a specific method.

```bash
roslyn-nav get-method --solution <path.sln> --file <path.cs> --method <MethodName>
# Or search across solution:
roslyn-nav get-method --solution <path.sln> --method <MethodName> --class <ClassName>
```

**When to use:** After using `list-class` to identify a method, extract just that method's code.

### 4. `find-usages` - Find All References

Find everywhere a symbol is used in the solution.

```bash
roslyn-nav find-usages --solution <path.sln> --symbol "ClassName.MethodName"
```

**When to use:** Before refactoring, to understand impact. Or to find examples of how something is used.

### 5. `list-classes` - List Classes in Namespace

Get all classes in a namespace.

```bash
roslyn-nav list-classes --solution <path.sln> --namespace <Namespace.Name>
```

**When to use:** To understand what classes exist in a module/namespace.

### 6. `get-namespace-structure` - Project Overview

Get the complete namespace hierarchy of a project.

```bash
roslyn-nav get-namespace-structure --solution <path.sln> --project <ProjectName>
```

**When to use:** To understand the overall structure of a project.

### 7. `get-methods` - Extract Multiple Methods

Get source code for multiple methods from a class at once.

```bash
roslyn-nav get-methods --solution <path.sln> --class <ClassName> --methods "Method1,Method2,Method3"
```

**When to use:** When you need to review several related methods from the same class.

### 8. `find-callers` - Find Method Callers

Find all methods that call a specific method.

```bash
roslyn-nav find-callers --solution <path.sln> --symbol "ClassName.MethodName"
```

**When to use:** To understand where a method is being called from, useful before modifying a method's signature or behavior.

### 9. `find-implementations` - Find Interface Implementations

Find all classes/structs that implement an interface.

```bash
roslyn-nav find-implementations --solution <path.sln> --interface <InterfaceName>
```

**When to use:** To find all implementations of an interface before modifying it, or to understand the available implementations.

### 10. `find-instantiations` - Find Class Instantiations

Find where a class is instantiated with `new`.

```bash
roslyn-nav find-instantiations --solution <path.sln> --class <ClassName>
```

**When to use:** To understand where objects are created, useful for DI analysis or factory patterns.

### 11. `find-by-attribute` - Search by Attribute

Find members decorated with a specific attribute.

```bash
roslyn-nav find-by-attribute --solution <path.sln> --attribute <AttributeName> [--pattern <TextPattern>]
```

**When to use:** To find all deprecated methods (`[Obsolete]`), API endpoints (`[HttpGet]`), test methods (`[Fact]`, `[Test]`), or Reqnroll steps (`[Given]`, `[When]`, `[Then]`).

### 12. `get-hierarchy` - Class Inheritance Hierarchy

Get the complete inheritance hierarchy of a class.

```bash
roslyn-nav get-hierarchy --solution <path.sln> --class <ClassName>
```

**When to use:** To understand class inheritance, what interfaces it implements, and what classes derive from it.

### 13. `get-constructor-deps` - Constructor Dependencies

Analyze constructor parameters for dependency injection.

```bash
roslyn-nav get-constructor-deps --solution <path.sln> --class <ClassName>
```

**When to use:** To understand what dependencies a class requires, useful when setting up DI or writing tests.

### 14. `check-overridable` - Check Method Modifiers

Check if a method is virtual, override, abstract, or sealed.

```bash
roslyn-nav check-overridable --solution <path.sln> --class <ClassName> --method <MethodName>
```

**When to use:** Before attempting to override a method, to verify it can be overridden.

### 15. `find-step-definition` - Find Step Definitions

Find Reqnroll/SpecFlow step definitions by text pattern.

```bash
roslyn-nav find-step-definition --solution <path.sln> --pattern "user is logged in"
```

**When to use:** To find BDD step definitions matching a text pattern. More targeted than `find-by-attribute` for step discovery.

### 16. `find-interface-consumers` - Find Interface Consumers

Find all implementations and injection points of an interface.

```bash
roslyn-nav find-interface-consumers --solution <path.sln> --interface <InterfaceName>
```

**When to use:** To see the full picture of how an interface is used - both who implements it and who depends on it via constructor injection, fields, or properties.

### 17. `list-feature-scenarios` - List Feature Scenarios

Parse .feature files (Gherkin) to list all scenarios.

```bash
roslyn-nav list-feature-scenarios --path <DirectoryPath>
```

**When to use:** To get an overview of all BDD scenarios in a directory, useful for understanding test coverage or finding specific scenarios.

## Recommended Workflows

### Workflow 1: Exploring Unknown Code

```bash
# 1. Get project structure
roslyn-nav get-namespace-structure --solution app.sln --project MyProject

# 2. List classes in relevant namespace
roslyn-nav list-classes --solution app.sln --namespace MyProject.Services

# 3. Get overview of interesting class
roslyn-nav list-class --solution app.sln --file Services/UserService.cs --class UserService

# 4. Read specific method (using line range from step 3)
# Use Read tool with offset/limit based on lineRange from list-class output
```

### Workflow 2: Understanding a Method

```bash
# 1. Find where it's defined
roslyn-nav find-symbol --solution app.sln --name ProcessData --kind method

# 2. Get the method source
roslyn-nav get-method --solution app.sln --method ProcessData --class DataProcessor

# 3. Find all usages
roslyn-nav find-usages --solution app.sln --symbol "DataProcessor.ProcessData"
```

### Workflow 3: Before Refactoring

```bash
# 1. Find all usages of the symbol you want to change
roslyn-nav find-usages --solution app.sln --symbol "OldClassName"

# 2. Review each usage location before making changes
```

### Workflow 4: Working with Interfaces

```bash
# 1. Find all implementations of an interface
roslyn-nav find-implementations --solution app.sln --interface IUserRepository

# 2. Get constructor dependencies of each implementation
roslyn-nav get-constructor-deps --solution app.sln --class SqlUserRepository

# 3. Get hierarchy to see if there's a base class
roslyn-nav get-hierarchy --solution app.sln --class SqlUserRepository
```

### Workflow 5: Before Modifying a Constructor

```bash
# 1. See current constructor dependencies
roslyn-nav get-constructor-deps --solution app.sln --class UserService

# 2. Find where the class is instantiated
roslyn-nav find-instantiations --solution app.sln --class UserService

# 3. Review each instantiation to update constructor calls
```

### Workflow 6: Working with Reqnroll/BDD Steps

```bash
# 1. Find step definitions matching a pattern (searches all step types)
roslyn-nav find-step-definition --solution app.sln --pattern "user is logged in"

# 2. Get the step method source
roslyn-nav get-method --solution app.sln --method "GivenUserIsLoggedIn" --class "AuthSteps"

# 3. List all scenarios in the features directory
roslyn-nav list-feature-scenarios --path tests/Features
```

### Workflow 7: Understanding Method Dependencies

```bash
# 1. Find who calls a method
roslyn-nav find-callers --solution app.sln --symbol "DataService.ProcessData"

# 2. Check if the method can be overridden
roslyn-nav check-overridable --solution app.sln --class DataService --method ProcessData

# 3. Find usages (includes more than just direct calls)
roslyn-nav find-usages --solution app.sln --symbol "DataService.ProcessData"
```

### Workflow 8: Understanding Interface Usage

```bash
# 1. Find all consumers of an interface (implementations + injections)
roslyn-nav find-interface-consumers --solution app.sln --interface IUserRepository

# 2. Get constructor dependencies for a specific implementation
roslyn-nav get-constructor-deps --solution app.sln --class SqlUserRepository

# 3. Find where implementations are instantiated
roslyn-nav find-instantiations --solution app.sln --class SqlUserRepository
```

## Output Format

All commands return JSON. Key fields:

- `lineRange`: [startLine, endLine] - 1-based line numbers for use with Read tool
- `filePath`: Relative path from solution directory
- `members`: Array of class members with their details

## Tips for Claude

1. **Always start with `list-class`** before reading a file - it tells you exactly which lines to read
2. **Use `find-symbol`** when you don't know where something is defined
3. **Use `find-usages`** before any refactoring to understand impact
4. **Use `find-implementations`** before modifying an interface
5. **Use `get-constructor-deps`** when setting up tests or DI
6. **Use `find-callers`** to understand the impact of changing a method
7. **Use `find-by-attribute`** to find deprecated code, API endpoints, or test methods
8. **Use `find-step-definition`** for BDD step discovery - more targeted than `find-by-attribute`
9. **Use `find-interface-consumers`** for complete interface usage analysis (implementations + injections)
10. **Use `list-feature-scenarios`** to get an overview of BDD test scenarios
11. **Combine with Read tool**: After getting lineRange from roslyn-nav, use `Read(file, offset=startLine, limit=endLine-startLine+1)`
12. **Cache awareness**: The tool caches solutions in memory, so subsequent commands on the same solution are faster

## Example Integration

Instead of:
```
Read entire 500-line file to find one method
```

Do this:
```bash
# Get class structure (returns line ranges)
roslyn-nav list-class --solution app.sln --file Services/BigService.cs --class BigService
# Output shows: GetUserAsync is at lines 45-62

# Then read only those lines
Read(file="Services/BigService.cs", offset=45, limit=18)
```

This reduces token usage from ~2000 tokens to ~200 tokens.
