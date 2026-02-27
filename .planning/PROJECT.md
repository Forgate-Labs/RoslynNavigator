# RoslynNavigator

## What This Is

Uma ferramenta CLI .NET global (`roslyn-nav`) que usa Roslyn para navegação semântica e mutação de código C#, e análise offline via snapshot SQLite. Projetada para assistentes de IA (Claude, GPT, Copilot) reduzirem o consumo de tokens em 85%+ ao explorar e modificar codebases C# — substituindo leituras de arquivos inteiros por consultas cirúrgicas e edições AST-aware, e permitindo análise de arquitetura/qualidade sem Roslyn em runtime.

## Core Value

O assistente de IA consegue navegar, criar, modificar e analisar código C# com precisão cirúrgica sem precisar ler arquivos inteiros — reduzindo tokens e eliminando edições ambíguas.

## Requirements

### Validated

- ✓ `list-class` — estrutura da classe com line ranges de todos os membros — v1.0
- ✓ `find-symbol` — localiza classes, métodos e propriedades na solution — v1.0
- ✓ `get-method` / `get-methods` — extrai source code de métodos específicos — v1.0
- ✓ `find-usages` — encontra todas as referências a um símbolo — v1.0
- ✓ `find-callers` — encontra quem chama um método — v1.0
- ✓ `find-implementations` — implementações de uma interface — v1.0
- ✓ `find-interface-consumers` — implementações + pontos de injeção de uma interface — v1.0
- ✓ `find-instantiations` — onde uma classe é instanciada — v1.0
- ✓ `find-by-attribute` — membros decorados com um atributo — v1.0
- ✓ `find-step-definition` — step definitions Reqnroll/SpecFlow por padrão de texto — v1.0
- ✓ `list-classes` — todas as classes em um namespace — v1.0
- ✓ `list-feature-scenarios` — cenários de arquivos .feature (Gherkin) — v1.0
- ✓ `get-namespace-structure` — hierarquia de namespaces de um projeto — v1.0
- ✓ `get-hierarchy` — hierarquia de herança de uma classe — v1.0
- ✓ `get-constructor-deps` — dependências do construtor para DI — v1.0
- ✓ `check-overridable` — verifica modificadores de método (virtual/override/abstract/sealed) — v1.0
- ✓ Plan/commit Unit of Work — IPlanStore, FilePlanStore, BackupService — v1.0
- ✓ `file read` / `file grep` — leitura imediata com line numbers e busca regex — v1.0
- ✓ `file plan edit/write/append/delete` — staged file mutations com validação — v1.0
- ✓ `file status/commit/rollback/clear` — atomic apply, unified diff, rollback — v1.0
- ✓ `dotnet scaffold class/interface/record/enum` — geração de arquivos C# — v1.0
- ✓ `dotnet add field/property/constructor/method/using` — inserção AST-aware — v1.0
- ✓ `dotnet update property/field` — substituição de membros existentes — v1.0
- ✓ `dotnet remove method/property/field` — remoção de membros pelo nome — v1.0
- ✓ CLAUDE.md documentado com todos os comandos write/mutation — v1.0
- ✓ `roslyn-nav snapshot` — gera SQLite com schema completo + sinais de análise — v2.1
- ✓ `roslyn-nav check` — avalia regras YAML builtin/domain com filtros severity/ruleId — v2.1
- ✓ `roslyn-nav snapshot query` — SQL arbitrário no snapshot com JSON output para LLM — v2.1
- ✓ Projetos `RoslynNavigator.Snapshot` e `RoslynNavigator.Rules` separados na solution — v2.1
- ✓ Integração baseline Sonar C# (`SonarQube.yaml`) + `check --rules` customizável — v2.1

### Active

_(empty — planning next milestone)_

### Out of Scope

- Geração de código baseada em IA (a ferramenta é a camada de execução, não de decisão)
- Suporte a outras linguagens além de C#
- Watcher de arquivos em tempo real
- Interface visual ou servidor HTTP
- Upgrade de pacotes NuGet existentes
- Projeto separado `RoslynNavigator.Ask` — a LLM permanece externa
- LLM embutida no binário `roslyn-nav`
- Paridade completa com SonarQube (sem taint/dataflow engine completo ainda)

## Context

**Shipped v1.0** com ~7.165 LOC C#, 122 arquivos, 42 dias.
**Shipped v2.1** com ~13.229 LOC C# total, ~87 arquivos modificados.

Tech stack: .NET 10, C#, `Microsoft.CodeAnalysis.CSharp`, `System.CommandLine`, `Microsoft.Data.Sqlite`, `YamlDotNet`, xUnit.

**Arquitetura atual:** 4 projetos na solution — `RoslynNavigator` (CLI), `RoslynNavigator.Snapshot` (lib), `RoslynNavigator.Rules` (lib), `RoslynNavigator.Tests` (xUnit). 201 testes passando. Total: 17 navegação + 24 write/mutation + 3 snapshot/rules/query = 44 comandos no CLI.

**Direções possíveis para v2.2+:**
- Suporte a regras de taint/dataflow para paridade maior com SonarQube
- Modo watch (`roslyn-nav snapshot --watch`) para snapshot incremental
- Exportação de snapshot para outros formatos (Parquet, CSV) para pipelines de dados

## Constraints

- **Tech stack**: .NET 10, C# — sem criar nova solution
- **Compatibilidade**: Pacotes NuGet existentes não mudam; output JSON contratos mantidos
- **Padrão existente**: Saída JSON em todos os comandos; seguir estrutura de `Commands/` existente
- **Atomicidade**: `file commit` aplica todas as mudanças ou nenhuma; backup sempre criado
- **Read-only snapshot**: `check` e `snapshot query` nunca mutam o arquivo `.db`

## Key Decisions

| Decision | Rationale | Outcome |
|----------|-----------|---------|
| Plan/commit pattern (Unit of Work) | Edições individuais são frágeis; commit atômico garante consistência | ✓ Good — zero arquivos corrompidos em todos os testes |
| Validação linha + old string | Evita ambiguidade quando a mesma string aparece múltiplas vezes | ✓ Good — recusa clara antes de staging |
| Grupos `file` e `dotnet` compartilham o mesmo estado de planos | Permite misturar edições raw e AST-aware no mesmo commit | ✓ Good — pipeline unificado simplifica rollback |
| `IPlanStore` + `FilePlanStore` | Testabilidade + persistência entre invocações | ✓ Good — testes xUnit usam InMemoryPlanStore |
| Namespace file-scoped por padrão no scaffold | C# 10+ padrão moderno | ✓ Good — consistente com codebase existente |
| `BaseTypeDeclarationSyntax` concrete-type switch | `BaseTypeDeclarationSyntax` não expõe `Members`/`WithMembers` diretamente | ✓ Good — dispatch funciona para class, record, struct |
| `BackupService` skips non-existent files | Seguro para criação de novos arquivos | ✓ Good — sem erros ao criar arquivos via scaffold |
| Detecção de indentação via `DetectIndentation` helper | Inserção de membros deve respeitar o estilo do arquivo existente | ✓ Good — funciona com tabs e spaces |
| `dotnet add using` ordena alphabetically | Mantém using block ordenado | ✓ Good — sem duplicatas, inserção limpa |
| `ParseUpdateRemoveMetadata` usa `TryGetProperty` para `content` | RemoveMember não tem campo content, UpdateMember tem | ✓ Good — comando único trata ambos os casos |
| Schema SQLite como embedded resource | Schema versionável junto ao código; migration idempotente | ✓ Good — schema carregado na inicialização sem config externa |
| `SqlReadOnlyGuard` antes de qualquer query | Impede mutação acidental do snapshot por regras mal-escritas | ✓ Good — guard testado com 61 casos |
| `RoslynNavigator.Snapshot` + `RoslynNavigator.Rules` como libs separadas | CLI evolution sem código monolítico; reuso futuro possível | ✓ Good — solution de 4 projetos, embedded resources migrados com libs |
| Baseline Sonar apenas high-confidence mappings | Evita false positives ruidosos; melhor experiência inicial | ✓ Good — catálogo present mas predicados só para regras com sinais confiáveis |
| Sinais de snapshot expandidos para suporte a regras de segurança | Predicados Sonar precisam de `parameter_count`, `uses_insecure_random`, etc | ✓ Good — snapshot mais rico sem breaking change no schema base |

---
*Last updated: 2026-02-27 after v2.1 milestone (Sonar Baseline Scope)*
