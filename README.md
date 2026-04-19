# Roslyn Navigator

A .NET global tool for semantic C# code navigation and targeted Roslyn-based mutation. Designed to reduce token usage by 85%+ when AI assistants explore and modify C# codebases.

## Why?

When AI assistants (Claude, GPT, Copilot) explore C# code, they typically read entire files. A 500-line file costs ~2000 tokens just to understand one method. And editing? Even worse — the AI rewrites the whole file to change one line.

Roslyn Navigator provides targeted commands that extract and mutate only what you need:
- Get class structure without reading the file
- Extract specific methods by name
- Find symbol definitions and usages semantically
- Navigate namespaces and project structure
- **Insert, update, and remove C# members** via Roslyn AST — no regex, no full rewrites
- Scaffold new C# types quickly from the CLI

**Result:** Read 18 lines instead of 500. Change one member without touching the rest.

## Installation

```bash
dotnet tool install --global RoslynNavigator
```

Verify installation:
```bash
roslyn-nav --help
```

## Quick Start

### Navigation

```bash
# Get overview of a class
roslyn-nav list-class --solution MyApp.sln --file Services/UserService.cs --class UserService

# Find where a class is defined
roslyn-nav find-symbol --solution MyApp.sln --name UserRepository --kind class

# Extract a specific method's source code
roslyn-nav get-method --solution MyApp.sln --method CreateUser --class UserService

# Find all usages of a method
roslyn-nav find-usages --solution MyApp.sln --symbol "UserService.CreateUser"
```

### Mutation

`dotnet` mutation commands write directly to disk.

```bash
# Scaffold new C# files
roslyn-nav dotnet scaffold class src/Services/UserService.cs MyApp.Services UserService
roslyn-nav dotnet scaffold interface src/Services/IUserService.cs MyApp.Services IUserService

# Insert members into existing types (fields → properties → constructors → methods)
roslyn-nav dotnet add using src/Services/UserService.cs Microsoft.Extensions.Logging
roslyn-nav dotnet add field src/Services/UserService.cs UserService private ILogger logger
roslyn-nav dotnet add property src/Services/UserService.cs UserService public string Name
roslyn-nav dotnet add constructor src/Services/UserService.cs UserService \
  "public UserService(ILogger<UserService> logger) { _logger = logger; }"
roslyn-nav dotnet add method src/Services/UserService.cs UserService \
  "public async Task<User> GetByIdAsync(int id) { return await _repo.FindAsync(id); }"

# Replace existing members
roslyn-nav dotnet update property src/Models/User.cs User Name \
  "public string Name { get; init; } = string.Empty;"
roslyn-nav dotnet update field src/Services/UserService.cs UserService _logger \
  "private readonly ILogger<UserService> _logger;"

# Remove members by name
roslyn-nav dotnet remove method src/Services/UserService.cs UserService ObsoleteHelper
roslyn-nav dotnet remove property src/Models/User.cs User DeprecatedField
```

## Commands

### Navigation

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

### Dotnet Scaffold

| Command | Description |
|---------|-------------|
| `dotnet scaffold class <path> <ns> <name>` | New class file with file-scoped namespace |
| `dotnet scaffold interface <path> <ns> <name>` | New interface file |
| `dotnet scaffold record <path> <ns> <name>` | New record file |
| `dotnet scaffold enum <path> <ns> <name>` | New enum file |

### Dotnet Add

| Command | Description |
|---------|-------------|
| `dotnet add using <path> <namespace>` | Add using directive (idempotent) |
| `dotnet add field <path> <class> <access> <type> <name>` | Insert field (auto-prepends `_`) |
| `dotnet add property <path> <class> <access> <type> <name>` | Insert `{ get; set; }` property |
| `dotnet add constructor <path> <class> <content>` | Insert constructor at correct position |
| `dotnet add method <path> <class> <content>` | Insert method before closing `}` |

### Dotnet Update & Remove

| Command | Description |
|---------|-------------|
| `dotnet update property <path> <class> <name> <content>` | Replace existing property declaration |
| `dotnet update field <path> <class> <name> <content>` | Replace existing field declaration |
| `dotnet remove method <path> <class> <name>` | Delete method by name |
| `dotnet remove property <path> <class> <name>` | Delete property by name |
| `dotnet remove field <path> <class> <name>` | Delete field by name (accepts `_name` or `name`) |

## Mutation Behavior

`dotnet scaffold`, `dotnet add`, `dotnet update`, and `dotnet remove` write immediately to disk.

For raw file reads or line-based edits, use your editor, shell, or agent tooling alongside `roslyn-nav`. The CLI now focuses on semantic navigation plus Roslyn-aware C# mutations.

## Output

Navigation commands output JSON. `dotnet` mutation commands print `DONE` on success.

Example JSON output:

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

This tool is designed for AI assistants. A ready-to-use [`SKILLS.md`](./SKILLS.md) file is included so agents can use `roslyn-nav` as an installed skill.

### Quick Setup

```bash
# After installing roslyn-nav globally, copy this file into your project root:
cp /path/to/RoslynNavigator/SKILLS.md .
```

That gives coding agents a focused reference for when and how to use `roslyn-nav` for semantic navigation and Roslyn-aware mutations.

## Performance

- **Cold start:** 2-5 seconds (loading MSBuild workspace)
- **Warm cache:** <500ms (solution cached in memory)
- **Token savings:** 85-98% reduction on navigation tasks
- **Write path:** Roslyn-aware type and member mutations from the CLI

## What's New in 3.0

- Streamlined the CLI to focus on semantic navigation.
- Kept the Roslyn-based `dotnet scaffold/add/update/remove` commands.
- Simplified the repository around the current command surface.

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
├── src/RoslynNavigator/         # CLI executable and command surface
├── tests/RoslynNavigator.Tests/ # Unit/integration test suite
├── tests/SampleSolution/        # Test solution for development
├── SKILLS.md                    # Skill instructions for coding agents
└── ROSLYN_TOOL_SPEC.md          # Original specification
```

## License

MIT

## Contributing

Contributions welcome! Please open an issue or PR.
