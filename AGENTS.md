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
| `find-usages` | Find all references | `--solution`, `--symbol` |
| `list-classes` | List classes in namespace | `--solution`, `--namespace` |
| `get-namespace-structure` | Get project structure | `--solution`, `--project` |

## Agent Decision Tree

```
Need to understand a codebase?
├── Don't know the structure → get-namespace-structure
├── Know the namespace → list-classes
├── Know the class → list-class
├── Need specific method → get-method
├── Need to find something → find-symbol
└── Need to understand usage → find-usages
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

## Best Practices for Agents

1. **Always use roslyn-nav before reading C# files** - Get the structure first
2. **Use lineRange for targeted reads** - The output tells you exactly which lines to read
3. **Prefer find-symbol over grep** - Semantic search is more accurate
4. **Use find-usages before refactoring** - Understand the impact first
5. **Cache the solution path** - Reuse it across multiple commands in the same session

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
