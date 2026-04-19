# roslyn-nav skill

Use `roslyn-nav` when working in a C#/.NET repository and you need semantic navigation instead of broad file reads.

## Prerequisite

Install the tool globally:

```bash
dotnet tool install --global RoslynNavigator
```

Verify:

```bash
roslyn-nav --help
```

## When to use

Use this skill for:
- finding symbols in a solution
- reading specific methods
- listing class structure and member line ranges
- finding usages, callers, implementations, instantiations, and hierarchy
- making Roslyn-aware C# mutations with `roslyn-nav dotnet *`

Prefer `roslyn-nav` before opening large `.cs` files.

## Core rules

- Always pass `--solution <path.sln>` on navigation commands.
- Start with `list-class`, `find-symbol`, or `get-method` before broad reads.
- Use `dotnet scaffold/add/update/remove` for C# mutations.
- `dotnet` mutation commands write immediately to disk.
- Run targeted tests after behavior-changing mutations.

## Typical workflow

```bash
# discover the solution
find . -name '*.sln'

# inspect a type
roslyn-nav list-class --solution MyApp.sln --file src/Services/UserService.cs --class UserService

# read one method only
roslyn-nav get-method --solution MyApp.sln --class UserService --method CreateUser

# find references
roslyn-nav find-usages --solution MyApp.sln --symbol "UserService.CreateUser"
```

## Navigation reference

```bash
roslyn-nav list-class --solution MyApp.sln --file path/to/File.cs --class ClassName
roslyn-nav find-symbol --solution MyApp.sln --name SymbolName --kind class|method|property
roslyn-nav get-method --solution MyApp.sln --class ClassName --method MethodName
roslyn-nav get-methods --solution MyApp.sln --class ClassName --methods "M1,M2,M3"
roslyn-nav find-usages --solution MyApp.sln --symbol "ClassName.MethodName"
roslyn-nav find-callers --solution MyApp.sln --symbol "ClassName.MethodName"
roslyn-nav find-implementations --solution MyApp.sln --interface IInterfaceName
roslyn-nav find-interface-consumers --solution MyApp.sln --interface IInterfaceName
roslyn-nav find-instantiations --solution MyApp.sln --class ClassName
roslyn-nav find-by-attribute --solution MyApp.sln --attribute AttributeName
roslyn-nav find-step-definition --solution MyApp.sln --pattern "step text"
roslyn-nav list-classes --solution MyApp.sln --namespace My.Namespace
roslyn-nav get-namespace-structure --solution MyApp.sln --project ProjectName
roslyn-nav get-hierarchy --solution MyApp.sln --class ClassName
roslyn-nav get-constructor-deps --solution MyApp.sln --class ClassName
roslyn-nav check-overridable --solution MyApp.sln --class ClassName --method MethodName
roslyn-nav list-feature-scenarios --path tests/Features
```

## Mutation reference

```bash
roslyn-nav dotnet scaffold class path/ClassName.cs My.Namespace ClassName
roslyn-nav dotnet scaffold interface path/IName.cs My.Namespace IName
roslyn-nav dotnet scaffold record path/RecordName.cs My.Namespace RecordName
roslyn-nav dotnet scaffold enum path/EnumName.cs My.Namespace EnumName

roslyn-nav dotnet add using path/File.cs My.Namespace
roslyn-nav dotnet add field path/File.cs ClassName private ILogger logger
roslyn-nav dotnet add property path/File.cs ClassName public string Name
roslyn-nav dotnet add constructor path/File.cs ClassName "public ClassName(ILogger<ClassName> logger) { _logger = logger; }"
roslyn-nav dotnet add method path/File.cs ClassName "public void Run() { }"

roslyn-nav dotnet update property path/File.cs ClassName Name "public string Name { get; set; } = string.Empty;"
roslyn-nav dotnet update field path/File.cs ClassName _logger "private readonly ILogger<ClassName> _logger;"

roslyn-nav dotnet remove method path/File.cs ClassName Run
roslyn-nav dotnet remove property path/File.cs ClassName Name
roslyn-nav dotnet remove field path/File.cs ClassName _logger
```

## Output behavior

- Navigation commands return JSON.
- `dotnet` mutation commands print `DONE` on success.
