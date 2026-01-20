# Roslyn Navigator

A .NET global tool for semantic C# code navigation using Roslyn. Designed to reduce token usage by 75%+ when AI assistants explore C# codebases.

## Why?

When AI assistants (Claude, GPT, Copilot) explore C# code, they typically read entire files. A 500-line file costs ~2000 tokens just to understand one method.

Roslyn Navigator provides targeted commands that extract only what you need:
- Get class structure without reading the file
- Extract specific methods by name
- Find symbol definitions and usages semantically
- Navigate namespaces and project structure

**Result:** Read 18 lines instead of 500. Use 100 tokens instead of 2000.

## Installation

```bash
dotnet tool install --global RoslynNavigator
```

Verify installation:
```bash
roslyn-nav --help
```

## Quick Start

```bash
# Get overview of a class
roslyn-nav list-class --solution MyApp.sln --file Services/UserService.cs --class UserService

# Find where a class is defined
roslyn-nav find-symbol --solution MyApp.sln --name UserRepository --kind class

# Extract a specific method's source code
roslyn-nav get-method --solution MyApp.sln --method CreateUser --class UserService

# Extract multiple methods at once
roslyn-nav get-methods --solution MyApp.sln --class UserService --methods "CreateUser,GetUser,DeleteUser"

# Find all usages of a method
roslyn-nav find-usages --solution MyApp.sln --symbol "UserService.CreateUser"

# Find methods that call another method
roslyn-nav find-callers --solution MyApp.sln --symbol "UserService.CreateUser"

# Find all implementations of an interface
roslyn-nav find-implementations --solution MyApp.sln --interface IUserRepository

# Find where a class is instantiated
roslyn-nav find-instantiations --solution MyApp.sln --class UserService

# Find members with a specific attribute
roslyn-nav find-by-attribute --solution MyApp.sln --attribute "Obsolete"

# Find Reqnroll/SpecFlow step definitions by pattern
roslyn-nav find-step-definition --solution MyApp.sln --pattern "user is logged in"

# Find all implementations and injection points of an interface
roslyn-nav find-interface-consumers --solution MyApp.sln --interface IUserRepository

# List all classes in a namespace
roslyn-nav list-classes --solution MyApp.sln --namespace MyApp.Services

# Get project namespace structure
roslyn-nav get-namespace-structure --solution MyApp.sln --project MyApp.Api

# Get class hierarchy (base types, interfaces, derived)
roslyn-nav get-hierarchy --solution MyApp.sln --class BaseController

# Analyze constructor dependencies
roslyn-nav get-constructor-deps --solution MyApp.sln --class UserService

# Check if a method is virtual/override
roslyn-nav check-overridable --solution MyApp.sln --class UserService --method GetUser

# List scenarios from Gherkin .feature files
roslyn-nav list-feature-scenarios --path tests/Features
```

## Commands

| Command | Description |
|---------|-------------|
| `list-class` | Get class structure (fields, properties, methods with line ranges) |
| `find-symbol` | Locate any symbol (class/method/property) in the solution |
| `get-method` | Extract complete source code of a method |
| `get-methods` | Extract multiple methods from a class at once |
| `find-usages` | Find all references to a symbol |
| `find-callers` | Find methods that call another method |
| `find-implementations` | Find all implementations of an interface |
| `find-interface-consumers` | Find interface implementations and injection points |
| `find-instantiations` | Find where a class is instantiated |
| `find-by-attribute` | Find members decorated with a specific attribute |
| `find-step-definition` | Find Reqnroll/SpecFlow step definitions by pattern |
| `list-classes` | List all classes in a namespace |
| `list-feature-scenarios` | List scenarios from Gherkin .feature files |
| `get-namespace-structure` | Get complete namespace hierarchy of a project |
| `get-hierarchy` | Get class inheritance hierarchy (base types, interfaces, derived types) |
| `get-constructor-deps` | Analyze constructor dependencies for DI |
| `check-overridable` | Check if a method is virtual/override/abstract/sealed |

## Output

All commands output JSON for easy parsing:

```json
{
  "className": "UserService",
  "namespace": "MyApp.Services",
  "lineRange": [10, 150],
  "members": [
    {
      "kind": "method",
      "name": "CreateUser",
      "lineRange": [45, 62],
      "signature": "public async Task<User> CreateUser(string name)"
    }
  ]
}
```

## AI Assistant Integration

This tool is designed for AI assistants. **Ready-to-use instruction files are included** - just copy them to your solution:

### For Claude Code

Copy [`CLAUDE.md`](./CLAUDE.md) to your solution root. It teaches Claude how to use roslyn-nav effectively with workflows and examples.

### For Other AI Agents (GPT, Copilot, etc.)

Copy [`AGENTS.md`](./AGENTS.md) to your solution root. It provides a generic reference for any AI agent with command reference, decision trees, and JSON schemas.

### Quick Setup

```bash
# In your solution directory:
cp /path/to/RoslynNavigator/CLAUDE.md .
cp /path/to/RoslynNavigator/AGENTS.md .
```

Now any AI assistant working on your codebase will know how to use roslyn-nav for efficient navigation.

## Typical Workflow

1. **Explore structure first:**
   ```bash
   roslyn-nav get-namespace-structure --solution app.sln --project MyProject
   ```

2. **Get class overview:**
   ```bash
   roslyn-nav list-class --solution app.sln --file Services/UserService.cs --class UserService
   ```

3. **Read specific lines** (using the lineRange from step 2):
   ```bash
   # The AI reads only lines 45-62 instead of the entire file
   ```

## Performance

- **Cold start:** 2-5 seconds (loading MSBuild workspace)
- **Warm cache:** <500ms (solution cached in memory)
- **Token savings:** 85-98% reduction on navigation tasks

## Requirements

- .NET 10 SDK or later
- Works with any C# solution (.sln)

## Building from Source

```bash
git clone https://github.com/Forgate-Labs/RoslynNavigator.git
cd RoslynNavigator
dotnet build
dotnet pack -c Release

# Install locally
dotnet tool install --global --add-source ./src/RoslynNavigator/bin/Release RoslynNavigator
```

## Project Structure

```
RoslynNavigator/
├── src/RoslynNavigator/     # Main tool source
├── tests/SampleSolution/    # Test solution for development
├── CLAUDE.md                # Instructions for Claude Code
├── AGENTS.md                # Instructions for AI agents
└── ROSLYN_TOOL_SPEC.md      # Original specification
```

## License

MIT

## Contributing

Contributions welcome! Please open an issue or PR.
