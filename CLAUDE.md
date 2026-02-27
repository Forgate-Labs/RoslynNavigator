# roslyn-nav — Referência Completa

`roslyn-nav` é um CLI .NET que usa Roslyn para navegar e mutar código C# sem ler arquivos inteiros. Todos os comandos retornam JSON. Use `--solution <path.sln>` em todos os comandos de navegação.

---

## Navegação

```bash
# Estrutura de uma classe (membros + lineRanges) — use SEMPRE antes de ler um arquivo
roslyn-nav list-class --solution app.sln --file path/to/File.cs --class ClassName

# Localizar símbolo na solution
roslyn-nav find-symbol --solution app.sln --name SymbolName --kind class|method|property

# Extrair source de um método
roslyn-nav get-method --solution app.sln --method MethodName --class ClassName

# Extrair múltiplos métodos de uma vez
roslyn-nav get-methods --solution app.sln --class ClassName --methods "M1,M2,M3"

# Todas as referências a um símbolo
roslyn-nav find-usages --solution app.sln --symbol "ClassName.MethodName"

# Quem chama um método
roslyn-nav find-callers --solution app.sln --symbol "ClassName.MethodName"

# Implementações de uma interface
roslyn-nav find-implementations --solution app.sln --interface IInterfaceName

# Implementações + pontos de injeção de uma interface
roslyn-nav find-interface-consumers --solution app.sln --interface IInterfaceName

# Onde uma classe é instanciada (new)
roslyn-nav find-instantiations --solution app.sln --class ClassName

# Membros com um atributo específico
roslyn-nav find-by-attribute --solution app.sln --attribute AttributeName

# Step definitions Reqnroll/SpecFlow por padrão de texto
roslyn-nav find-step-definition --solution app.sln --pattern "user is logged in"

# Classes em um namespace
roslyn-nav list-classes --solution app.sln --namespace My.Namespace

# Hierarquia de namespaces de um projeto
roslyn-nav get-namespace-structure --solution app.sln --project ProjectName

# Herança de uma classe (base types, interfaces, derived)
roslyn-nav get-hierarchy --solution app.sln --class ClassName

# Dependências do construtor (DI)
roslyn-nav get-constructor-deps --solution app.sln --class ClassName

# Verificar se método é virtual/override/abstract/sealed
roslyn-nav check-overridable --solution app.sln --class ClassName --method MethodName

# Cenários de arquivos .feature (Gherkin)
roslyn-nav list-feature-scenarios --path tests/Features
```

---

## Write & Mutation — Pipeline Stage → Commit

**Regra:** todos os comandos write/dotnet são *staged* em `.roslyn-nav-plans.json`. Nada toca o disco até `file commit`.

```
stage ops  →  file status  →  file commit  →  (file rollback se necessário)
```

### File Read (imediato, sem staging)

```bash
roslyn-nav file read path/to/File.cs                  # arquivo inteiro com line numbers
roslyn-nav file read path/to/File.cs --lines 10-30    # só o range
roslyn-nav file grep "pattern" src/ --ext .cs --max-lines 50
```

### File Stage

```bash
# Edit: valida que linha N contém <old> antes de aceitar — falha rápido se não bater
roslyn-nav file plan edit path/File.cs <lineN> "<old content>" "<new content>"

# Write: cria ou sobrescreve o arquivo inteiro
roslyn-nav file plan write path/File.cs "<full content>"

# Append: adiciona ao final
roslyn-nav file plan append path/File.cs "<content>"

# Delete: remove linha N, valida <old>
roslyn-nav file plan delete path/File.cs <lineN> "<old content>"
```

### File Commit / Rollback

```bash
roslyn-nav file status           # unified diff de tudo staged (preview)
roslyn-nav file commit           # cria backup em .roslyn-nav-backup/<ts>/, valida tudo, aplica atomicamente
roslyn-nav file rollback         # restaura todos os arquivos do último backup
roslyn-nav file clear            # descarta todos os staged ops sem tocar arquivos
```

### Dotnet Scaffold (staged)

```bash
roslyn-nav dotnet scaffold class     path/ClassName.cs     My.Namespace ClassName
roslyn-nav dotnet scaffold interface path/IName.cs         My.Namespace IName
roslyn-nav dotnet scaffold record    path/RecordName.cs    My.Namespace RecordName
roslyn-nav dotnet scaffold enum      path/EnumName.cs      My.Namespace EnumName
# depois: roslyn-nav file commit
```

### Dotnet Add (staged)

```bash
# using: idempotente, insere em ordem alfabética
roslyn-nav dotnet add using path/File.cs My.Namespace

# field: prepende _ automaticamente (passe o nome sem _)
roslyn-nav dotnet add field path/File.cs ClassName private ILogger logger
# → insere: private ILogger _logger;

# property
roslyn-nav dotnet add property path/File.cs ClassName public string Name

# constructor: passe a assinatura + corpo completos
roslyn-nav dotnet add constructor path/File.cs ClassName \
  "public ClassName(ILogger<ClassName> logger) { _logger = logger; }"

# method: passe a assinatura + corpo completos
roslyn-nav dotnet add method path/File.cs ClassName \
  "public async Task<User> GetByIdAsync(int id) { return await _repo.FindAsync(id); }"

# ordem de inserção respeitada: fields → properties → constructors → methods
```

### Dotnet Update / Remove (staged)

```bash
# update: substitui a declaração inteira do membro
roslyn-nav dotnet update property path/File.cs ClassName PropName \
  "public string PropName { get; init; } = string.Empty;"

roslyn-nav dotnet update field path/File.cs ClassName _fieldName \
  "private readonly ILogger<ClassName> _fieldName;"

# remove: apaga o membro pelo nome
roslyn-nav dotnet remove method   path/File.cs ClassName MethodName
roslyn-nav dotnet remove property path/File.cs ClassName PropName
roslyn-nav dotnet remove field    path/File.cs ClassName _fieldName  # aceita com ou sem _
```

---

## Regras essenciais

1. **Navegação antes de ler:** use `list-class` para obter `lineRange` dos membros; depois `file read --lines` só naquele range.
2. **Stage → status → commit:** nunca pule o `file status` antes do `file commit` em mudanças críticas.
3. **`file plan edit` falha rápido:** se a linha não contiver `<old>`, a operação é rejeitada imediatamente — nenhum arquivo é tocado.
4. **Atomicidade:** se qualquer validação falhar no `file commit`, zero arquivos são modificados.
5. **`dotnet add using` é idempotente:** chame sem verificar se já existe.
6. **`dotnet update/remove` encontra pelo nome:** não importa a linha atual, Roslyn localiza o nó.
7. **Misture raw e AST:** `file plan edit` e `dotnet add/update/remove` compartilham o mesmo store — um único `file commit` aplica tudo.
