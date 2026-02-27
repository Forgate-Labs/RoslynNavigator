# roslyn-nav — Complete Reference

`roslyn-nav` is a .NET CLI that uses Roslyn to navigate and mutate C# code without reading entire files. All commands return JSON. Use `--solution <path.sln>` on all navigation commands.

---

## Navigation

```bash
# Class structure (members + lineRanges) — use ALWAYS before reading a file
roslyn-nav list-class --solution app.sln --file path/to/File.cs --class ClassName

# Locate a symbol in the solution
roslyn-nav find-symbol --solution app.sln --name SymbolName --kind class|method|property

# Extract a method's source code
roslyn-nav get-method --solution app.sln --method MethodName --class ClassName

# Extract multiple methods at once
roslyn-nav get-methods --solution app.sln --class ClassName --methods "M1,M2,M3"

# All references to a symbol
roslyn-nav find-usages --solution app.sln --symbol "ClassName.MethodName"

# Who calls a method
roslyn-nav find-callers --solution app.sln --symbol "ClassName.MethodName"

# Implementations of an interface
roslyn-nav find-implementations --solution app.sln --interface IInterfaceName

# Implementations + injection points of an interface
roslyn-nav find-interface-consumers --solution app.sln --interface IInterfaceName

# Where a class is instantiated (new)
roslyn-nav find-instantiations --solution app.sln --class ClassName

# Members decorated with a specific attribute
roslyn-nav find-by-attribute --solution app.sln --attribute AttributeName

# Reqnroll/SpecFlow step definitions by text pattern
roslyn-nav find-step-definition --solution app.sln --pattern "user is logged in"

# Classes in a namespace
roslyn-nav list-classes --solution app.sln --namespace My.Namespace

# Namespace hierarchy of a project
roslyn-nav get-namespace-structure --solution app.sln --project ProjectName

# Class inheritance hierarchy (base types, interfaces, derived)
roslyn-nav get-hierarchy --solution app.sln --class ClassName

# Constructor dependencies (DI)
roslyn-nav get-constructor-deps --solution app.sln --class ClassName

# Check if a method is virtual/override/abstract/sealed
roslyn-nav check-overridable --solution app.sln --class ClassName --method MethodName

# Scenarios from Gherkin .feature files
roslyn-nav list-feature-scenarios --path tests/Features
```

---

## Write & Mutation — Stage → Commit Pipeline

**Rule:** all write/dotnet commands are *staged* in `.roslyn-nav-plans.json`. Nothing touches disk until `file commit`.

```
stage ops  →  file status  →  file commit  →  (file rollback if needed)
```

### File Read (immediate, no staging)

```bash
roslyn-nav file read path/to/File.cs                  # whole file with line numbers
roslyn-nav file read path/to/File.cs --lines 10-30    # range only
roslyn-nav file grep "pattern" src/ --ext .cs --max-lines 50
```

### File Stage

```bash
# Edit: validates that line N contains <old> before accepting — fails fast if mismatch
roslyn-nav file plan edit path/File.cs <lineN> "<old content>" "<new content>"

# Write: creates or overwrites the entire file
roslyn-nav file plan write path/File.cs "<full content>"

# Append: adds content to end of file
roslyn-nav file plan append path/File.cs "<content>"

# Delete: removes line N, validates <old>
roslyn-nav file plan delete path/File.cs <lineN> "<old content>"
```

### File Commit / Rollback

```bash
roslyn-nav file status           # unified diff of all staged ops (preview)
roslyn-nav file commit           # creates backup in .roslyn-nav-backup/<ts>/, validates all, applies atomically
roslyn-nav file rollback         # restores all files from last backup
roslyn-nav file clear            # discards all staged ops without touching files
```

### Dotnet Scaffold (staged)

```bash
roslyn-nav dotnet scaffold class     path/ClassName.cs     My.Namespace ClassName
roslyn-nav dotnet scaffold interface path/IName.cs         My.Namespace IName
roslyn-nav dotnet scaffold record    path/RecordName.cs    My.Namespace RecordName
roslyn-nav dotnet scaffold enum      path/EnumName.cs      My.Namespace EnumName
# then: roslyn-nav file commit
```

### Dotnet Add (staged)

```bash
# using: idempotent, inserted in alphabetical order
roslyn-nav dotnet add using path/File.cs My.Namespace

# field: auto-prepends _ (pass name without _)
roslyn-nav dotnet add field path/File.cs ClassName private ILogger logger
# → inserts: private ILogger _logger;

# property
roslyn-nav dotnet add property path/File.cs ClassName public string Name

# constructor: pass full signature + body
roslyn-nav dotnet add constructor path/File.cs ClassName \
  "public ClassName(ILogger<ClassName> logger) { _logger = logger; }"

# method: pass full signature + body
roslyn-nav dotnet add method path/File.cs ClassName \
  "public async Task<User> GetByIdAsync(int id) { return await _repo.FindAsync(id); }"

# insertion order enforced: fields → properties → constructors → methods
```

### Dotnet Update / Remove (staged)

```bash
# update: replaces the entire member declaration
roslyn-nav dotnet update property path/File.cs ClassName PropName \
  "public string PropName { get; init; } = string.Empty;"

roslyn-nav dotnet update field path/File.cs ClassName _fieldName \
  "private readonly ILogger<ClassName> _fieldName;"

# remove: deletes member by name
roslyn-nav dotnet remove method   path/File.cs ClassName MethodName
roslyn-nav dotnet remove property path/File.cs ClassName PropName
roslyn-nav dotnet remove field    path/File.cs ClassName _fieldName  # accepts with or without _
```

---

## Essential Rules

1. **Navigate before reading:** use `list-class` to get member `lineRange`; then `file read --lines` only that range.
2. **Stage → status → commit:** always run `file status` before `file commit` on critical changes.
3. **`file plan edit` fails fast:** if line N does not contain `<old>`, the op is rejected immediately — no file is touched.
4. **Atomicity:** if any validation fails during `file commit`, zero files are modified.
5. **`dotnet add using` is idempotent:** call it without checking whether the directive already exists.
6. **`dotnet update/remove` finds by name:** current line number doesn't matter, Roslyn locates the node.
7. **Mix raw and AST:** `file plan edit` and `dotnet add/update/remove` share the same store — a single `file commit` applies everything.
