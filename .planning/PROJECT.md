# RoslynNavigator

## What This Is

Uma ferramenta CLI .NET global (`roslyn-nav`) que usa Roslyn para navegação semântica e mutação de código C#. Projetada para assistentes de IA (Claude, GPT, Copilot) reduzirem o consumo de tokens em 85%+ ao explorar e modificar codebases C# — substituindo leituras de arquivos inteiros por consultas cirúrgicas e edições AST-aware.

## Core Value

O assistente de IA consegue navegar, criar e modificar código C# com precisão cirúrgica sem precisar ler arquivos inteiros — reduzindo tokens e eliminando edições ambíguas.

## Current Milestone: v2.0 Snapshot, Rules & Ask

**Goal:** Expandir o Roslyn Navigator para análise completa de solution via snapshot SQLite, avaliação de regras YAML e consulta SQL direta para suporte a perguntas em linguagem natural.

**Target features:**
- Comando `snapshot` para gerar banco SQLite com classes, métodos, dependências, chamadas, annotations, flags e metadados
- Comando `check` para aplicar regras builtin/domain e reportar violações filtráveis por severidade/regra
- Comando `snapshot query` para executar SQL arbitrário no snapshot e retornar JSON para consumo da LLM
- Novos projetos `RoslynNavigator.Snapshot` e `RoslynNavigator.Rules` integrados à solution existente
- Registro dos novos comandos no CLI mantendo padrões existentes de output JSON e arquitetura

## Requirements

### Validated

- ✓ `list-class` — estrutura da classe com line ranges de todos os membros
- ✓ `find-symbol` — localiza classes, métodos e propriedades na solution
- ✓ `get-method` / `get-methods` — extrai source code de métodos específicos
- ✓ `find-usages` — encontra todas as referências a um símbolo
- ✓ `find-callers` — encontra quem chama um método
- ✓ `find-implementations` — implementações de uma interface
- ✓ `find-interface-consumers` — implementações + pontos de injeção de uma interface
- ✓ `find-instantiations` — onde uma classe é instanciada
- ✓ `find-by-attribute` — membros decorados com um atributo
- ✓ `find-step-definition` — step definitions Reqnroll/SpecFlow por padrão de texto
- ✓ `list-classes` — todas as classes em um namespace
- ✓ `list-feature-scenarios` — cenários de arquivos .feature (Gherkin)
- ✓ `get-namespace-structure` — hierarquia de namespaces de um projeto
- ✓ `get-hierarchy` — hierarquia de herança de uma classe
- ✓ `get-constructor-deps` — dependências do construtor para DI
- ✓ `check-overridable` — verifica modificadores de método (virtual/override/abstract/sealed)
- ✓ Plan/commit Unit of Work — IPlanStore, FilePlanStore, BackupService — v1.0
- ✓ `file read` / `file grep` — leitura imediata com line numbers e busca regex — v1.0
- ✓ `file plan edit/write/append/delete` — staged file mutations com validação — v1.0
- ✓ `file status/commit/rollback/clear` — atomic apply, unified diff, rollback — v1.0
- ✓ `dotnet scaffold class/interface/record/enum` — geração de arquivos C# — v1.0
- ✓ `dotnet add field/property/constructor/method/using` — inserção AST-aware — v1.0
- ✓ `dotnet update property/field` — substituição de membros existentes — v1.0
- ✓ `dotnet remove method/property/field` — remoção de membros pelo nome — v1.0
- ✓ CLAUDE.md documentado com todos os comandos write/mutation — v1.0

### Active

- [ ] Entregar `snapshot` com Walker estrutural/semântico, detector de padrões e persistência SQLite
- [ ] Entregar `check` com RuleLoader, QueryBuilder e RuleEvaluator genéricos para YAML
- [ ] Entregar `snapshot query` para consulta SQL direta com output JSON consistente
- [ ] Integrar os novos projetos/comandos sem quebrar os 41 comandos atuais

### Out of Scope

- Geração de código baseada em IA (a ferramenta é a camada de execução, não de decisão)
- Suporte a outras linguagens além de C#
- Watcher de arquivos em tempo real
- Interface visual ou servidor HTTP
- Upgrade de pacotes NuGet existentes
- Alteração dos comandos de navegação existentes

## Context

**Shipped v1.0** com ~7.165 LOC C#, 122 arquivos modificados em 42 dias.
Tech stack: .NET 10, C#, `Microsoft.CodeAnalysis.CSharp`, `System.CommandLine`, xUnit.

A superfície de mutação completa está entregue: `file` group (10 subcomandos) + `dotnet` group (scaffold 4 + add 5 + update 2 + remove 3 = 14 subcomandos). Total: 17 navegação + 24 write/mutation = 41 comandos no CLI.

O principal desafio técnico — `dotnet add method` com detecção de indentação e ordem convencional de membros — foi resolvido via `BaseTypeDeclarationSyntax` concrete-type switch expression.

## Constraints

- **Tech stack**: .NET 10, C# — sem criar nova solution
- **Compatibilidade**: Pacotes NuGet existentes não mudam
- **Padrão existente**: Saída JSON em todos os comandos; seguir estrutura de `Commands/` existente
- **Atomicidade**: `file commit` aplica todas as mudanças ou nenhuma; backup sempre criado

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

---
*Last updated: 2026-02-27 after starting milestone v2.0 Snapshot, Rules & Ask*
