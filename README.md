# Roslyn Navigator

A .NET global tool for semantic C# code navigation **and mutation** using Roslyn. Designed to reduce token usage by 85%+ when AI assistants explore and modify C# codebases.

## Why?

When AI assistants (Claude, GPT, Copilot) explore C# code, they typically read entire files. A 500-line file costs ~2000 tokens just to understand one method. And editing? Even worse — the AI rewrites the whole file to change one line.

Roslyn Navigator provides targeted commands that extract and mutate only what you need:
- Get class structure without reading the file
- Extract specific methods by name
- Find symbol definitions and usages semantically
- Navigate namespaces and project structure
- **Stage file edits atomically** with diff preview and rollback
- **Insert, update, and remove C# members** via Roslyn AST — no regex, no full rewrites
- **Generate SQLite snapshots** of your solution for fast analysis
- **Run rules packs** (architecture, code quality, security) against snapshots
- **Query snapshots with SQL** and get stable JSON output for LLM workflows

**Result:** Read 18 lines instead of 500. Edit one method without touching the rest.

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

### Write & Mutation

All write commands are **staged** — nothing touches disk until `file commit`.

```bash
# Read a file with line numbers
roslyn-nav file read Services/UserService.cs --lines 10-30

# Search across files
roslyn-nav file grep "ILogger" src/ --ext .cs

# Stage a line edit
roslyn-nav file plan edit Services/UserService.cs 12 "    private long _count;"

# Stage sequential multi-line replacement via \n
roslyn-nav file plan edit Services/UserService.cs 20 "if (x == null)\n{\n    return;\n}"

# Preview all staged changes as unified diff
roslyn-nav file status

# Apply atomically (backup created first)
roslyn-nav file commit

# Undo last commit
roslyn-nav file rollback

# Discard staged ops without writing anything
roslyn-nav file clear

# Scaffold new C# files
roslyn-nav dotnet scaffold class src/Services/UserService.cs MyApp.Services UserService
roslyn-nav dotnet scaffold interface src/Services/IUserService.cs MyApp.Services IUserService
roslyn-nav file commit

# Insert members into existing types (respects fields → properties → constructors → methods order)
roslyn-nav dotnet add using src/Services/UserService.cs Microsoft.Extensions.Logging
roslyn-nav dotnet add field src/Services/UserService.cs UserService private ILogger logger
roslyn-nav dotnet add property src/Services/UserService.cs UserService public string Name
roslyn-nav dotnet add constructor src/Services/UserService.cs UserService \
  "public UserService(ILogger<UserService> logger) { _logger = logger; }"
roslyn-nav dotnet add method src/Services/UserService.cs UserService \
  "public async Task<User> GetByIdAsync(int id) { return await _repo.FindAsync(id); }"
roslyn-nav file commit

# Replace existing members
roslyn-nav dotnet update property src/Models/User.cs User Name \
  "public string Name { get; init; } = string.Empty;"
roslyn-nav dotnet update field src/Services/UserService.cs UserService _logger \
  "private readonly ILogger<UserService> _logger;"
roslyn-nav file commit

# Remove members by name
roslyn-nav dotnet remove method src/Services/UserService.cs UserService ObsoleteHelper
roslyn-nav dotnet remove property src/Models/User.cs User DeprecatedField
roslyn-nav file commit
```

### Snapshot, Rules, and Query

```bash
# Generate snapshot database (default output in workspace)
roslyn-nav snapshot --solution MyApp.sln

# Run builtin + optional domain rules against snapshot
roslyn-nav check --db ./.roslyn-nav/snapshot.db

# Filter violations
roslyn-nav check --severity error --ruleId no-naked-strings

# Run read-only SQL queries against snapshot
roslyn-nav snapshot query --sql "SELECT name, namespace FROM classes LIMIT 10"
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

### Snapshot & Rules

| Command | Description |
|---------|-------------|
| `snapshot --solution <path.sln> [--db <path>]` | Generate/update SQLite snapshot with classes, methods, dependencies, calls, annotations and analysis flags |
| `check [--db <path>] [--severity <level>] [--ruleId <id>]` | Evaluate YAML rules against snapshot and return violations as JSON |
| `snapshot query --sql "<SELECT...>" [--db <path>]` | Execute read-only SQL against snapshot and return JSON array of rows |

### File Read

| Command | Description |
|---------|-------------|
| `file read <path> [--lines START-END]` | File content with line numbers; optional range filter |
| `file grep <pattern> [path] [--ext .cs] [--max-lines N]` | Regex search with extension filter |

### File Stage / Commit

| Command | Description |
|---------|-------------|
| `file plan edit <path> <line> <new>` | Stage a line or sequential multi-line replacement (use `\n`); returns `DONE` |
| `file plan write <path> <content>` | Stage a full file overwrite (creates if missing) |
| `file plan append <path> <content>` | Stage an append to end of file |
| `file plan delete <path> <line> <old>` | Stage a line deletion — validates old content |
| `file status [--json]` | Preview all staged changes as unified diff |
| `file commit [--json]` | Create backup, validate, apply atomically |
| `file rollback` | Restore all files from last backup |
| `file clear` | Discard all staged ops without writing anything |

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

## Plan / Commit Workflow

Write and dotnet mutation commands accumulate in `.roslyn-nav-plans.json`. Nothing is written to disk until `file commit`.

```
stage ops  →  file status  →  file commit  →  (file rollback if needed)
```

`file commit` always creates a timestamped backup in `.roslyn-nav-backup/<timestamp>/` before touching any file. If any validation fails, zero files are modified.

## Output

All commands output JSON:

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

This tool is designed for AI assistants. **Ready-to-use instruction files are included** — copy them to your solution root:

### For Claude Code

Copy [`CLAUDE.md`](./CLAUDE.md) to your solution root. It teaches Claude how to use roslyn-nav effectively with workflows, examples, and write command tips.

### For Other AI Agents (GPT, Copilot, etc.)

Copy [`AGENTS.md`](./AGENTS.md) to your solution root. It provides a generic reference for any AI agent with command reference, decision trees, and JSON schemas.

### Quick Setup

```bash
# In your solution directory:
cp /path/to/RoslynNavigator/CLAUDE.md .
cp /path/to/RoslynNavigator/AGENTS.md .
```

Now any AI assistant working on your codebase will know how to use roslyn-nav for efficient navigation and mutation.

## Performance

- **Cold start:** 2-5 seconds (loading MSBuild workspace)
- **Warm cache:** <500ms (solution cached in memory)
- **Token savings:** 85-98% reduction on navigation tasks
- **Edit safety:** atomic commit + automatic backup on every `file commit`

## What's New (Phases 1-4)

- **Phase 1 - Snapshot Foundation:** `snapshot` command now builds a SQLite database with schema for classes, methods, dependencies, calls, annotations, flags, and metadata.
- **Phase 2 - Rules Engine:** `check` command now evaluates builtin YAML rule packs plus optional domain rules and supports severity/rule filters.
- **Phase 3 - Query Integration:** `snapshot query` enables arbitrary read-only SQL over snapshot data with stable JSON output for automation/LLM use.
- **Phase 4 - Integration & Polish:** solution split into modular projects (`RoslynNavigator`, `RoslynNavigator.Snapshot`, `RoslynNavigator.Rules`, `RoslynNavigator.Tests`) with backward compatibility preserved.

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
├── src/RoslynNavigator/             # CLI executable and command surface
├── src/RoslynNavigator.Snapshot/    # Snapshot extraction + SQLite schema/services
├── src/RoslynNavigator.Rules/       # Rules loading, SQL compilation, evaluation
├── tests/RoslynNavigator.Tests/     # Unit/integration test suite
├── tests/SampleSolution/    # Test solution for development
├── CLAUDE.md                # Instructions for Claude Code
├── AGENTS.md                # Instructions for AI agents
└── ROSLYN_TOOL_SPEC.md      # Original specification
```

## License

MIT

## Contributing

Contributions welcome! Please open an issue or PR.
