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

## Output Format

All commands return JSON. Key fields:

- `lineRange`: [startLine, endLine] - 1-based line numbers for use with Read tool
- `filePath`: Relative path from solution directory
- `members`: Array of class members with their details

## Tips for Claude

1. **Always start with `list-class`** before reading a file - it tells you exactly which lines to read
2. **Use `find-symbol`** when you don't know where something is defined
3. **Use `find-usages`** before any refactoring to understand impact
4. **Combine with Read tool**: After getting lineRange from roslyn-nav, use `Read(file, offset=startLine, limit=endLine-startLine+1)`
5. **Cache awareness**: The tool caches solutions in memory, so subsequent commands on the same solution are faster

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
