# AGENTS.md - Roslyn Navigator for AI Agents

This file provides instructions for AI agents (Claude, GPT, Copilot, etc.) on how to use the `roslyn-nav` tool for efficient C# codebase navigation.

## Tool Overview

`roslyn-nav` is a .NET global tool that provides semantic C# code analysis. It reduces token consumption by 85%+ by enabling targeted code extraction instead of full-file reads.

## Prerequisites

The tool must be installed:
```bash
dotnet tool install --global RoslynNavigator
```

## Command Reference

| Command | Purpose | Key Options |
|---------|---------|-------------|
| `list-class` | Get class structure overview | `--solution`, `--file`, `--class` |
| `find-symbol` | Locate symbol definition | `--solution`, `--name`, `--kind` |
| `get-method` | Extract method source code | `--solution`, `--file`, `--method`, `--class` |
| `get-methods` | Extract multiple methods at once | `--solution`, `--class`, `--methods` |
| `find-usages` | Find all references | `--solution`, `--symbol` |
| `find-callers` | Find methods that call another method | `--solution`, `--symbol` |
| `find-implementations` | Find interface implementations | `--solution`, `--interface` |
| `find-instantiations` | Find class instantiations | `--solution`, `--class` |
| `find-by-attribute` | Find by attribute decoration | `--solution`, `--attribute`, `--pattern` |
| `list-classes` | List classes in namespace | `--solution`, `--namespace` |
| `get-namespace-structure` | Get project structure | `--solution`, `--project` |
| `get-hierarchy` | Get inheritance hierarchy | `--solution`, `--class` |
| `get-constructor-deps` | Analyze constructor dependencies | `--solution`, `--class` |
| `check-overridable` | Check method modifiers | `--solution`, `--class`, `--method` |

## Agent Decision Tree

```
Need to understand a codebase?
├── Don't know the structure → get-namespace-structure
├── Know the namespace → list-classes
├── Know the class → list-class
├── Need specific method → get-method
├── Need multiple methods → get-methods
├── Need to find something → find-symbol
├── Need to understand usage → find-usages
└── Need to find callers → find-callers

Working with interfaces?
├── Find implementations → find-implementations
└── Check hierarchy → get-hierarchy

Refactoring a class?
├── Find instantiations → find-instantiations
├── Analyze constructor → get-constructor-deps
└── Check if overridable → check-overridable

Finding specific code?
├── By attribute → find-by-attribute
├── By name → find-symbol
└── By usage → find-usages
```

## Efficient Patterns

### Pattern 1: Targeted Reading
```bash
# Instead of reading entire file:
# BAD: Read("Services/UserService.cs")  # 500 lines, ~2000 tokens

# Get structure first:
roslyn-nav list-class --solution app.sln --file Services/UserService.cs --class UserService
# Output: { "members": [{ "name": "GetUser", "lineRange": [45, 62] }] }

# GOOD: Read("Services/UserService.cs", offset=45, limit=18)  # 18 lines, ~100 tokens
```

### Pattern 2: Symbol Discovery
```bash
# When asked "where is X defined?"
roslyn-nav find-symbol --solution app.sln --name UserRepository --kind class
# Returns file path and line range
```

### Pattern 3: Impact Analysis
```bash
# Before modifying a method, find all callers:
roslyn-nav find-usages --solution app.sln --symbol "UserService.CreateUser"
# Returns all files and lines where it's called
```

### Pattern 4: Interface Analysis
```bash
# Find all implementations of an interface:
roslyn-nav find-implementations --solution app.sln --interface IUserRepository
# Returns all classes/structs implementing the interface

# Then analyze each implementation:
roslyn-nav get-constructor-deps --solution app.sln --class SqlUserRepository
```

### Pattern 5: Constructor Analysis
```bash
# Understand DI dependencies:
roslyn-nav get-constructor-deps --solution app.sln --class UserService
# Returns all constructors with parameter types

# Find where the class is instantiated:
roslyn-nav find-instantiations --solution app.sln --class UserService
```

### Pattern 6: Attribute-Based Search
```bash
# Find deprecated code:
roslyn-nav find-by-attribute --solution app.sln --attribute "Obsolete"

# Find API endpoints:
roslyn-nav find-by-attribute --solution app.sln --attribute "HttpGet"

# Find Reqnroll steps with pattern:
roslyn-nav find-by-attribute --solution app.sln --attribute "Given" --pattern "user logged in"
```

### Pattern 7: Hierarchy Analysis
```bash
# Understand class relationships:
roslyn-nav get-hierarchy --solution app.sln --class BaseController
# Returns base types, interfaces, and derived types
```

## JSON Output Structure

### list-class
```json
{
  "className": "string",
  "namespace": "string",
  "lineRange": [startLine, endLine],
  "filePath": "relative/path.cs",
  "members": [
    {
      "kind": "field|property|method|constructor",
      "name": "string",
      "lineRange": [start, end],
      "signature": "string",
      "accessibility": "public|private|protected|internal"
    }
  ]
}
```

### find-symbol
```json
{
  "symbolName": "string",
  "kind": "class|method|property",
  "results": [
    {
      "filePath": "string",
      "lineRange": [start, end],
      "namespace": "string",
      "fullName": "string"
    }
  ]
}
```

### find-usages
```json
{
  "symbolName": "string",
  "totalUsages": number,
  "usages": [
    {
      "filePath": "string",
      "line": number,
      "column": number,
      "contextCode": "the line of code",
      "methodContext": "containing method name"
    }
  ]
}
```

### find-implementations
```json
{
  "interface": "string",
  "implementations": [
    {
      "name": "string",
      "kind": "class|struct|record",
      "filePath": "string",
      "line": number,
      "namespace": "string"
    }
  ],
  "totalCount": number
}
```

### find-instantiations
```json
{
  "className": "string",
  "instantiations": [
    {
      "filePath": "string",
      "line": number,
      "containingMethod": "string",
      "containingClass": "string",
      "contextCode": "the line of code"
    }
  ],
  "totalCount": number
}
```

### find-callers
```json
{
  "symbol": "string",
  "callers": [
    {
      "callerClass": "string",
      "callerMethod": "string",
      "filePath": "string",
      "line": number,
      "contextCode": "the line of code"
    }
  ],
  "totalCount": number
}
```

### find-by-attribute
```json
{
  "attribute": "string",
  "pattern": "string|null",
  "matches": [
    {
      "memberType": "method|class|property|field|parameter",
      "name": "string",
      "attributeArguments": "[Attribute(args)]",
      "filePath": "string",
      "line": number,
      "containingClass": "string",
      "namespace": "string"
    }
  ],
  "totalCount": number
}
```

### get-hierarchy
```json
{
  "className": "string",
  "filePath": "string",
  "namespace": "string",
  "baseTypes": ["string"],
  "interfaces": ["string"],
  "derivedTypes": [
    {
      "name": "string",
      "kind": "class|record",
      "filePath": "string",
      "line": number,
      "namespace": "string"
    }
  ]
}
```

### get-constructor-deps
```json
{
  "className": "string",
  "filePath": "string",
  "namespace": "string",
  "constructors": [
    {
      "parameters": [
        {
          "name": "string",
          "type": "string",
          "fullTypeName": "string"
        }
      ],
      "lineRange": [start, end],
      "signature": "string"
    }
  ]
}
```

### check-overridable
```json
{
  "className": "string",
  "methodName": "string",
  "isVirtual": boolean,
  "isOverride": boolean,
  "isAbstract": boolean,
  "isSealed": boolean,
  "canBeOverridden": boolean,
  "baseMethod": "string|null",
  "filePath": "string",
  "line": number
}
```

### get-methods
```json
{
  "className": "string",
  "filePath": "string",
  "methods": [
    {
      "name": "string",
      "signature": "string",
      "lineRange": [start, end],
      "sourceCode": "string",
      "returnType": "string",
      "parameters": [{"name": "string", "type": "string"}],
      "accessibility": "string",
      "isAsync": boolean
    }
  ]
}
```

## Best Practices for Agents

1. **Always use roslyn-nav before reading C# files** - Get the structure first
2. **Use lineRange for targeted reads** - The output tells you exactly which lines to read
3. **Prefer find-symbol over grep** - Semantic search is more accurate
4. **Use find-usages before refactoring** - Understand the impact first
5. **Use find-implementations before modifying interfaces** - Know all affected code
6. **Use get-constructor-deps for DI analysis** - Understand dependencies
7. **Use find-callers to understand method impact** - Different from find-usages (callers vs all references)
8. **Use find-by-attribute for BDD/Reqnroll steps** - Pattern matching makes it powerful
9. **Cache the solution path** - Reuse it across multiple commands in the same session

## Error Handling

All errors return JSON:
```json
{
  "success": false,
  "error": {
    "code": "error_code",
    "message": "Human readable message"
  }
}
```

Common errors:
- `File not found` - Check the file path relative to solution
- `Class not found` - Verify class name spelling
- `Project not found` - Check project name (without .csproj extension)

## Performance Notes

- First command on a solution takes 2-5 seconds (loading workspace)
- Subsequent commands on same solution are <500ms (cached)
- The cache persists for the tool's process lifetime
