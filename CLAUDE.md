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

## Write & Mutation Commands

These commands stage operations in `.roslyn-nav-plans.json` (in the current working directory). Nothing is written to disk until `file commit` is called.

### Plan / Commit Workflow

All write and dotnet mutation commands are **staged** — they accumulate in a local plan file. Apply them atomically with `file commit`.

```bash
# Stage one or more operations
roslyn-nav dotnet add field MyClass.cs MyClass private string name
roslyn-nav dotnet add property MyClass.cs MyClass public string Name

# Preview the diff before committing
roslyn-nav file status

# Apply atomically (creates backup in .roslyn-nav-backup/ first)
roslyn-nav file commit

# Undo the last commit if something went wrong
roslyn-nav file rollback

# Discard staged ops without applying
roslyn-nav file clear
```

---

### `file read` — Read File with Line Numbers

```bash
roslyn-nav file read <path> [--lines START-END]
```

Returns file content with 1-based line numbers. Use `--lines` to extract a range (output from `roslyn-nav list-class` provides `lineRange` values).

```bash
# Read entire file
roslyn-nav file read src/MyService.cs

# Read only lines 45 to 62
roslyn-nav file read src/MyService.cs --lines 45-62
```

---

### `file grep` — Search File Content

```bash
roslyn-nav file grep <pattern> [path] [--ext <.ext>] [--max-lines <N>]
```

Regex search. Returns matching lines with file path and line number. Defaults to current directory, `.cs` extension, 100 max results.

```bash
# Find all usages of ILogger in .cs files
roslyn-nav file grep "ILogger" src/ --ext .cs

# Search for TODO comments, limit to 20 results
roslyn-nav file grep "TODO" --max-lines 20
```

---

### `file plan edit` — Stage a Line Edit

```bash
roslyn-nav file plan edit <path> <lineNumber> <oldContent> <newContent>
```

Deterministic edit: validates that line `<lineNumber>` contains exactly `<oldContent>` before staging. Fails fast if the line does not match (prevents silent wrong-line edits).

```bash
# Replace line 12 — must contain the exact string shown
roslyn-nav file plan edit src/MyService.cs 12 "    private int _count;" "    private long _count;"
```

---

### `file plan write` — Stage a Full File Overwrite

```bash
roslyn-nav file plan write <path> <content>
```

Stages a full file overwrite. Creates the file if it does not exist. No validation required — always accepted.

```bash
roslyn-nav file plan write src/Generated.cs "namespace MyApp;\npublic class Generated { }"
```

---

### `file plan append` — Stage Content Append

```bash
roslyn-nav file plan append <path> <content>
```

Stages appending `<content>` to the end of the file. Always accepted.

```bash
roslyn-nav file plan append src/MyClass.cs "\n    // end of file marker"
```

---

### `file plan delete` — Stage a Line Deletion

```bash
roslyn-nav file plan delete <path> <lineNumber> <oldContent>
```

Stages deletion of line `<lineNumber>`. Validates the line contains `<oldContent>` before accepting.

```bash
roslyn-nav file plan delete src/MyClass.cs 15 "    private int _unused;"
```

---

### `file status` — Preview Staged Changes

```bash
roslyn-nav file status [--json]
```

Shows a unified diff of all staged operations without touching any file. Use `--json` for machine-readable output.

```bash
roslyn-nav file status
```

---

### `file commit` — Apply All Staged Operations

```bash
roslyn-nav file commit [--json]
```

Creates a timestamped backup in `.roslyn-nav-backup/`, validates all staged operations (fails fast if any validation fails — zero files modified), then applies all changes atomically. Returns a unified diff. Use `--json` for structured output.

```bash
roslyn-nav file commit
```

---

### `file rollback` — Restore Last Commit

```bash
roslyn-nav file rollback
```

Restores all files touched by the last `file commit` from the backup directory. Does not clear the backup path — multiple rollbacks are safe.

```bash
roslyn-nav file rollback
```

---

### `file clear` — Discard All Staged Operations

```bash
roslyn-nav file clear
```

Deletes `.roslyn-nav-plans.json` and discards all staged operations without touching any file.

```bash
roslyn-nav file clear
```

---

### `dotnet scaffold class` — Generate a New Class File

```bash
roslyn-nav dotnet scaffold class <path> <namespace> <className>
```

Stages creation of a new `.cs` file with file-scoped namespace and a minimal `public class` body.

```bash
roslyn-nav dotnet scaffold class src/Services/UserService.cs MyApp.Services UserService
roslyn-nav file commit
```

---

### `dotnet scaffold interface` — Generate a New Interface File

```bash
roslyn-nav dotnet scaffold interface <path> <namespace> <interfaceName>
```

```bash
roslyn-nav dotnet scaffold interface src/Services/IUserService.cs MyApp.Services IUserService
```

---

### `dotnet scaffold record` — Generate a New Record File

```bash
roslyn-nav dotnet scaffold record <path> <namespace> <recordName>
```

```bash
roslyn-nav dotnet scaffold record src/Models/UserDto.cs MyApp.Models UserDto
```

---

### `dotnet scaffold enum` — Generate a New Enum File

```bash
roslyn-nav dotnet scaffold enum <path> <namespace> <enumName>
```

```bash
roslyn-nav dotnet scaffold enum src/Models/UserRole.cs MyApp.Models UserRole
```

---

### `dotnet add using` — Add a Using Directive

```bash
roslyn-nav dotnet add using <path> <namespace>
```

Adds `using <namespace>;` to the top of the file (sorted alphabetically). No-op if the directive already exists — safe to call unconditionally.

```bash
roslyn-nav dotnet add using src/Services/UserService.cs System.Collections.Generic
```

---

### `dotnet add field` — Add a Field to a Type

```bash
roslyn-nav dotnet add field <path> <className> <access> <type> <name>
```

Inserts `<access> <type> _<name>;` after the last existing field in the target class/record/struct. Leading underscore is added automatically — provide the base name without it.

```bash
roslyn-nav dotnet add field src/Services/UserService.cs UserService private ILogger logger
# Inserts: private ILogger _logger;
```

---

### `dotnet add property` — Add a Property to a Type

```bash
roslyn-nav dotnet add property <path> <className> <access> <type> <name>
```

Inserts `<access> <type> <name> { get; set; }` after the last existing property.

```bash
roslyn-nav dotnet add property src/Models/User.cs User public string Email
# Inserts: public string Email { get; set; }
```

---

### `dotnet add constructor` — Add a Constructor to a Type

```bash
roslyn-nav dotnet add constructor <path> <className> <content>
```

Inserts the full constructor source (signature + body) at the correct position (after properties/fields). Validates syntax before staging.

```bash
roslyn-nav dotnet add constructor src/Services/UserService.cs UserService \
  "public UserService(ILogger<UserService> logger) { _logger = logger; }"
```

---

### `dotnet add method` — Add a Method to a Type

```bash
roslyn-nav dotnet add method <path> <className> <content>
```

Inserts the full method source (signature + body) before the closing brace of the type body. Detects existing indentation. Validates syntax before staging.

```bash
roslyn-nav dotnet add method src/Services/UserService.cs UserService \
  "public async Task<User> GetByIdAsync(int id) { return await _repo.FindAsync(id); }"
```

---

### `dotnet update property` — Replace a Property

```bash
roslyn-nav dotnet update property <path> <className> <propertyName> <content>
```

Replaces the named property's entire declaration with `<content>`. Validates that the member exists and that `<content>` is syntactically valid. Returns an error if the property is not found.

```bash
roslyn-nav dotnet update property src/Models/User.cs User Name \
  "public string Name { get; init; } = string.Empty;"
roslyn-nav file commit
```

---

### `dotnet update field` — Replace a Field

```bash
roslyn-nav dotnet update field <path> <className> <fieldName> <content>
```

Replaces the named field's entire declaration. Accepts the field name with or without leading underscore (e.g., `_count` or `count` both match `private int _count;`).

```bash
roslyn-nav dotnet update field src/Services/UserService.cs UserService _logger \
  "private readonly ILogger<UserService> _logger;"
roslyn-nav file commit
```

---

### `dotnet remove method` — Delete a Method

```bash
roslyn-nav dotnet remove method <path> <className> <methodName>
```

Removes the named method from the type body. Returns an error if the method is not found.

```bash
roslyn-nav dotnet remove method src/Services/UserService.cs UserService ObsoleteHelper
roslyn-nav file commit
```

---

### `dotnet remove property` — Delete a Property

```bash
roslyn-nav dotnet remove property <path> <className> <propertyName>
```

```bash
roslyn-nav dotnet remove property src/Models/User.cs User DeprecatedField
```

---

### `dotnet remove field` — Delete a Field

```bash
roslyn-nav dotnet remove field <path> <className> <fieldName>
```

Accepts the field name with or without leading underscore.

```bash
roslyn-nav dotnet remove field src/Services/UserService.cs UserService _oldCache
roslyn-nav file commit
```

---

## Write Command Tips for Claude

1. **Stage first, inspect, then commit** — use `file status` to preview the unified diff before `file commit`
2. **`file plan edit` is safest for small changes** — validates the old content matches before accepting, preventing wrong-line edits
3. **`dotnet add/update/remove` go through the same commit pipeline** — mix AST mutations and raw edits in a single `file commit`
4. **Prefer `dotnet update property/field`** over `file plan edit` for member replacement — Roslyn finds the exact node regardless of line number changes
5. **`dotnet add using` is idempotent** — safe to call even if the directive already exists
6. **Field name underscore** — `dotnet add field` auto-prepends `_`; `dotnet update field` and `dotnet remove field` accept both `name` and `_name`
7. **Syntax validation happens at commit time** — `dotnet add/update` with invalid content will fail `file commit` validation and leave all files untouched
8. **`file rollback` after a bad commit** — restores all files from the pre-commit backup; safe to call multiple times
