# Roslyn Navigator - .NET Tool Specification

## Goal
Build a global .NET tool that provides semantic C# code navigation using Roslyn to reduce token usage when exploring codebases. Replace full-file reads with targeted symbol lookups via CLI.

## What is a .NET Tool?
A packable console application that can be installed globally or locally:
```bash
# Install globally
dotnet tool install --global roslyn-navigator

# Use anywhere
roslyn-nav list-class --file src/Api/Servicos/ServicoOnboarding.cs --class ServicoOnboarding
```

## Stack
- .NET 10 (C#)
- Microsoft.CodeAnalysis.CSharp 4.x (Roslyn)
- Microsoft.CodeAnalysis.Workspaces.MSBuild
- System.CommandLine for CLI parsing
- Packaged as `<PackAsTool>true</PackAsTool>`

## Commands to Implement

### 1. `list-class`
**Purpose**: Get overview of a class without reading full file

```bash
roslyn-nav list-class \
  --solution McpAssistenteDatabase.sln \
  --file src/McpAssistenteDatabase.Api/Servicos/ServicoOnboarding.cs \
  --class ServicoOnboarding
```

**Output (JSON)**:
```json
{
  "className": "ServicoOnboarding",
  "namespace": "McpAssistenteDatabase.Api.Servicos",
  "lineRange": [9, 480],
  "filePath": "src/McpAssistenteDatabase.Api/Servicos/ServicoOnboarding.cs",
  "members": [
    {
      "kind": "field",
      "name": "_contexto",
      "type": "BancoDeDadosContexto",
      "line": 11,
      "accessibility": "private",
      "isReadonly": true
    },
    {
      "kind": "method",
      "name": "ProcessarCodigoConviteAsync",
      "lineRange": [291, 332],
      "signature": "private async Task<string> ProcessarCodigoConviteAsync(SessaoOnboarding sessao, string mensagem)",
      "accessibility": "private",
      "isAsync": true,
      "returnType": "Task<string>",
      "parameters": [
        { "name": "sessao", "type": "SessaoOnboarding" },
        { "name": "mensagem", "type": "string" }
      ]
    },
    {
      "kind": "property",
      "name": "SomeProperty",
      "type": "string",
      "line": 15,
      "accessibility": "public",
      "hasGetter": true,
      "hasSetter": false
    }
  ]
}
```

### 2. `find-symbol`
**Purpose**: Locate any symbol (class/method/property) in the solution

```bash
roslyn-nav find-symbol \
  --solution McpAssistenteDatabase.sln \
  --name ProcessadorMcp \
  --kind class
```

**Output (JSON)**:
```json
{
  "symbolName": "ProcessadorMcp",
  "kind": "class",
  "results": [
    {
      "filePath": "src/McpAssistenteDatabase.Api/Mcp/ProcessadorMcp.cs",
      "lineRange": [9, 255],
      "namespace": "McpAssistenteDatabase.Api.Mcp",
      "fullName": "McpAssistenteDatabase.Api.Mcp.ProcessadorMcp"
    }
  ]
}
```

### 3. `get-method`
**Purpose**: Read ONLY a specific method's code

```bash
roslyn-nav get-method \
  --solution McpAssistenteDatabase.sln \
  --file src/McpAssistenteDatabase.Api/Mcp/ProcessadorMcp.cs \
  --method ExecutarAsync
```

**Output (JSON)**:
```json
{
  "methodName": "ExecutarAsync",
  "className": "ProcessadorMcp",
  "lineRange": [31, 226],
  "filePath": "src/McpAssistenteDatabase.Api/Mcp/ProcessadorMcp.cs",
  "signature": "public async Task<RespostaMcp> ExecutarAsync(RequisicaoMcp requisicao)",
  "accessibility": "public",
  "isAsync": true,
  "returnType": "Task<RespostaMcp>",
  "parameters": [
    { "name": "requisicao", "type": "RequisicaoMcp" }
  ],
  "sourceCode": "    public async Task<RespostaMcp> ExecutarAsync(RequisicaoMcp requisicao)\n    {\n        if (string.IsNullOrWhiteSpace(requisicao.Nome))\n        {\n            return RespostaMcp.ComErro(\"ferramenta_invalida\", \"Nome da ferramenta e obrigatorio\");\n        }\n..."
}
```

### 4. `find-usages`
**Purpose**: Where is this symbol used?

```bash
roslyn-nav find-usages \
  --solution McpAssistenteDatabase.sln \
  --symbol "NormalizadorNumeroWhatsApp.NormalizarNumeroBrasileiro"
```

**Output (JSON)**:
```json
{
  "symbolName": "NormalizadorNumeroWhatsApp.NormalizarNumeroBrasileiro",
  "totalUsages": 2,
  "usages": [
    {
      "filePath": "src/McpAssistenteDatabase.Api/Servicos/ServicoFuncionarios.cs",
      "line": 48,
      "column": 37,
      "contextCode": "            numeroNormalizado = NormalizadorNumeroWhatsApp.NormalizarNumeroBrasileiro(entrada.Ddd, entrada.Telefone);",
      "methodContext": "CadastrarAsync"
    },
    {
      "filePath": "tests/SomeTest.cs",
      "line": 25,
      "column": 20,
      "contextCode": "        var result = NormalizadorNumeroWhatsApp.NormalizarNumeroBrasileiro(\"11\", \"987654321\");",
      "methodContext": "Test_NormalizarNumero"
    }
  ]
}
```

### 5. `list-classes`
**Purpose**: Get all classes in a namespace or project

```bash
roslyn-nav list-classes \
  --solution McpAssistenteDatabase.sln \
  --namespace McpAssistenteDatabase.Api.Servicos
```

**Output (JSON)**:
```json
{
  "namespace": "McpAssistenteDatabase.Api.Servicos",
  "totalClasses": 12,
  "classes": [
    {
      "name": "ServicoCaixa",
      "filePath": "src/McpAssistenteDatabase.Api/Servicos/ServicoCaixa.cs",
      "lineRange": [8, 245],
      "accessibility": "public",
      "isStatic": false
    },
    {
      "name": "ServicoFuncionarios",
      "filePath": "src/McpAssistenteDatabase.Api/Servicos/ServicoFuncionarios.cs",
      "lineRange": [7, 105],
      "accessibility": "public",
      "isStatic": false
    }
  ]
}
```

### 6. `get-namespace-structure`
**Purpose**: Get complete namespace hierarchy

```bash
roslyn-nav get-namespace-structure \
  --solution McpAssistenteDatabase.sln \
  --project McpAssistenteDatabase.Api
```

**Output (JSON)**:
```json
{
  "projectName": "McpAssistenteDatabase.Api",
  "namespaces": [
    {
      "name": "McpAssistenteDatabase.Api.Servicos",
      "classCount": 12,
      "classes": ["ServicoCaixa", "ServicoEstoque", "ServicoFuncionarios"]
    },
    {
      "name": "McpAssistenteDatabase.Api.Entidades",
      "classCount": 15,
      "classes": ["Usuario", "Empresa", "Funcionario"]
    },
    {
      "name": "McpAssistenteDatabase.Api.Mcp",
      "classCount": 5,
      "classes": ["ProcessadorMcp", "FerramentasMcp"]
    }
  ]
}
```

## Project Structure

```
roslyn-navigator/
├── src/
│   └── RoslynNavigator/
│       ├── Program.cs                    # CLI entry point with System.CommandLine
│       ├── RoslynNavigator.csproj        # <PackAsTool>true</PackAsTool>
│       ├── Services/
│       │   ├── WorkspaceService.cs       # Manages MSBuildWorkspace and caching
│       │   └── RoslynAnalyzer.cs         # Core Roslyn analysis logic
│       ├── Commands/
│       │   ├── ListClassCommand.cs
│       │   ├── FindSymbolCommand.cs
│       │   ├── GetMethodCommand.cs
│       │   ├── FindUsagesCommand.cs
│       │   ├── ListClassesCommand.cs
│       │   └── GetNamespaceStructureCommand.cs
│       └── Models/
│           ├── ClassStructure.cs
│           ├── SymbolInfo.cs
│           └── MethodInfo.cs
└── README.md
```

## Key Implementation Details

### 1. System.CommandLine Setup

```csharp
using System.CommandLine;

var rootCommand = new RootCommand("Roslyn Navigator - C# code analysis tool");

// list-class command
var listClassCommand = new Command("list-class", "Get class structure overview");
listClassCommand.AddOption(new Option<string>("--solution", "Path to .sln file") { IsRequired = true });
listClassCommand.AddOption(new Option<string>("--file", "Path to .cs file") { IsRequired = true });
listClassCommand.AddOption(new Option<string>("--class", "Class name"));
listClassCommand.SetHandler(async (string solution, string file, string className) =>
{
    var result = await ListClassHandler.ExecuteAsync(solution, file, className);
    Console.WriteLine(JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true }));
},
solutionOption, fileOption, classOption);

rootCommand.AddCommand(listClassCommand);

return await rootCommand.InvokeAsync(args);
```

### 2. Workspace Caching Strategy

```csharp
public class WorkspaceService
{
    private static readonly Dictionary<string, (MSBuildWorkspace workspace, Solution solution)> _cache = new();
    private static readonly SemaphoreSlim _lock = new(1, 1);

    public static async Task<Solution> GetSolutionAsync(string solutionPath)
    {
        await _lock.WaitAsync();
        try
        {
            if (!_cache.ContainsKey(solutionPath))
            {
                var workspace = MSBuildWorkspace.Create();
                var solution = await workspace.OpenSolutionAsync(solutionPath);
                _cache[solutionPath] = (workspace, solution);
            }
            return _cache[solutionPath].solution;
        }
        finally
        {
            _lock.Release();
        }
    }
}
```

### 3. Roslyn Analysis Examples

```csharp
// Get class members
public static async Task<ClassStructure> AnalyzeClassAsync(Document document, string className)
{
    var syntaxRoot = await document.GetSyntaxRootAsync();
    var classNode = syntaxRoot.DescendantNodes()
        .OfType<ClassDeclarationSyntax>()
        .FirstOrDefault(c => c.Identifier.Text == className);

    if (classNode == null) return null;

    var members = new List<MemberInfo>();

    // Fields
    foreach (var field in classNode.Members.OfType<FieldDeclarationSyntax>())
    {
        foreach (var variable in field.Declaration.Variables)
        {
            members.Add(new MemberInfo
            {
                Kind = "field",
                Name = variable.Identifier.Text,
                Type = field.Declaration.Type.ToString(),
                Line = field.GetLocation().GetLineSpan().StartLinePosition.Line + 1,
                Accessibility = GetAccessibility(field.Modifiers),
                IsReadonly = field.Modifiers.Any(SyntaxKind.ReadOnlyKeyword)
            });
        }
    }

    // Methods
    foreach (var method in classNode.Members.OfType<MethodDeclarationSyntax>())
    {
        var lineSpan = method.GetLocation().GetLineSpan();
        members.Add(new MemberInfo
        {
            Kind = "method",
            Name = method.Identifier.Text,
            LineRange = new[] { lineSpan.StartLinePosition.Line + 1, lineSpan.EndLinePosition.Line + 1 },
            Signature = GetMethodSignature(method),
            Accessibility = GetAccessibility(method.Modifiers),
            IsAsync = method.Modifiers.Any(SyntaxKind.AsyncKeyword),
            ReturnType = method.ReturnType.ToString(),
            Parameters = method.ParameterList.Parameters.Select(p => new ParameterInfo
            {
                Name = p.Identifier.Text,
                Type = p.Type?.ToString() ?? "var"
            }).ToList()
        });
    }

    return new ClassStructure
    {
        ClassName = className,
        Namespace = GetNamespace(classNode),
        LineRange = new[] {
            classNode.GetLocation().GetLineSpan().StartLinePosition.Line + 1,
            classNode.GetLocation().GetLineSpan().EndLinePosition.Line + 1
        },
        Members = members
    };
}

// Find symbol usages
public static async Task<List<UsageInfo>> FindUsagesAsync(Solution solution, string symbolName)
{
    var usages = new List<UsageInfo>();

    foreach (var project in solution.Projects)
    {
        var compilation = await project.GetCompilationAsync();

        foreach (var tree in compilation.SyntaxTrees)
        {
            var semanticModel = compilation.GetSemanticModel(tree);
            var root = await tree.GetRootAsync();

            var identifiers = root.DescendantNodes()
                .OfType<IdentifierNameSyntax>()
                .Where(i => i.Identifier.Text == symbolName.Split('.').Last());

            foreach (var identifier in identifiers)
            {
                var symbolInfo = semanticModel.GetSymbolInfo(identifier);
                if (symbolInfo.Symbol?.ToString() == symbolName)
                {
                    var lineSpan = identifier.GetLocation().GetLineSpan();
                    var line = lineSpan.StartLinePosition.Line + 1;
                    var lineText = tree.GetText().Lines[lineSpan.StartLinePosition.Line].ToString();

                    usages.Add(new UsageInfo
                    {
                        FilePath = tree.FilePath,
                        Line = line,
                        Column = lineSpan.StartLinePosition.Character + 1,
                        ContextCode = lineText.Trim(),
                        MethodContext = GetContainingMethodName(identifier)
                    });
                }
            }
        }
    }

    return usages;
}
```

### 4. Package Configuration (.csproj)

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net10.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>

    <!-- .NET Tool Configuration -->
    <PackAsTool>true</PackAsTool>
    <ToolCommandName>roslyn-nav</ToolCommandName>
    <PackageId>RoslynNavigator</PackageId>
    <Version>1.0.0</Version>
    <Authors>Eduardo</Authors>
    <Description>CLI tool for semantic C# code navigation using Roslyn</Description>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Microsoft.CodeAnalysis.CSharp" Version="4.11.0" />
    <PackageReference Include="Microsoft.CodeAnalysis.Workspaces.MSBuild" Version="4.11.0" />
    <PackageReference Include="System.CommandLine" Version="2.0.0-beta4.22272.1" />
  </ItemGroup>
</Project>
```

## Installation & Usage

```bash
# Build and pack locally
cd roslyn-navigator/src/RoslynNavigator
dotnet pack

# Install globally
dotnet tool install --global --add-source ./nupkg RoslynNavigator

# Verify installation
roslyn-nav --version

# Use it!
cd /path/to/McpAssistenteDatabase
roslyn-nav list-class \
  --solution McpAssistenteDatabase.sln \
  --file src/McpAssistenteDatabase.Api/Servicos/ServicoOnboarding.cs \
  --class ServicoOnboarding
```

## Performance Requirements

- **Cold start** (first run on solution): < 5 seconds
- **Warm cache** (subsequent runs): < 500ms
- **Memory usage**: Keep workspace in memory, max ~500MB for large solutions
- **Output**: Always valid JSON (use `System.Text.Json`)

## Claude Integration Pattern

Claude Code can invoke via Bash tool:
```bash
roslyn-nav list-class \
  --solution McpAssistenteDatabase.sln \
  --file src/Api/Servicos/ServicoOnboarding.cs \
  --class ServicoOnboarding
```

Response is parsed JSON, Claude reads only specific line ranges based on output.

## Success Criteria

After implementation, these workflows should work:

**Workflow 1**: Find and read a specific method
```bash
# Step 1: Find the class
roslyn-nav find-symbol --solution app.sln --name ProcessadorMcp --kind class
# Returns: { "filePath": "src/Mcp/ProcessadorMcp.cs", "lineRange": [9, 255] }

# Step 2: List methods in that class
roslyn-nav list-class --solution app.sln --file src/Mcp/ProcessadorMcp.cs --class ProcessadorMcp
# Returns: { "members": [ { "name": "ExecutarAsync", "lineRange": [31, 226] } ] }

# Step 3: Claude reads ONLY lines 31-226
Read(file_path="src/Mcp/ProcessadorMcp.cs", offset=31, limit=196)
```

**Workflow 2**: Find all usages before refactoring
```bash
roslyn-nav find-usages \
  --solution app.sln \
  --symbol "NormalizadorNumeroWhatsApp.NormalizarNumeroBrasileiro"
# Returns all 2 files where it's used
```

## Token Savings Target

| Task | Before (full read) | After (tool + targeted read) | Savings |
|------|-------------------|------------------------------|---------|
| "Where is method X?" | 25,000 tokens | 3,500 tokens | **86%** |
| "List all classes" | 100,000 tokens | 2,000 tokens | **98%** |
| "Find usages of Y" | 50,000 tokens | 5,000 tokens | **90%** |

**Overall goal**: 85%+ token reduction on code navigation tasks.

## What NOT to Build

- ❌ Code generation/modification (read-only tool)
- ❌ Intellisense/autocomplete (too complex)
- ❌ Multi-language support (C# only for now)
- ❌ Real-time file watching (one-shot analysis)
- ❌ Visual UI (CLI only)

## Next Steps

1. Create new project: `dotnet new console -n RoslynNavigator`
2. Add packages: Roslyn, System.CommandLine
3. Implement commands one by one
4. Test on McpAssistenteDatabase.sln
5. Package and install globally
6. Measure token savings in real usage

Build this as a production-ready .NET global tool that can be shared and reused across projects.
