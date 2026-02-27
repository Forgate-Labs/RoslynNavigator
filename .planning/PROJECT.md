# RoslynNavigator

## What This Is

Uma ferramenta CLI .NET global (`roslyn-nav`) que usa Roslyn para navegação semântica e mutação de código C#. Projetada para assistentes de IA (Claude, GPT, Copilot) reduzirem o consumo de tokens em 85%+ ao explorar e modificar codebases C# — substituindo leituras de arquivos inteiros por consultas cirúrgicas e edições AST-aware.

## Core Value

O assistente de IA consegue navegar, criar e modificar código C# com precisão cirúrgica sem precisar ler arquivos inteiros — reduzindo tokens e eliminando edições ambíguas.

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

### Active

- [ ] **Grupo `file`**: CRUD de arquivos com padrão plan/commit (Unit of Work)
  - [ ] `file read` — leitura imediata com line numbers e range opcional
  - [ ] `file grep` — busca de padrão com filtro de extensão
  - [ ] `file plan edit` — edição determinística (linha + old string)
  - [ ] `file plan write` — cria ou sobrescreve arquivo
  - [ ] `file plan append` — adiciona linha ao final
  - [ ] `file plan delete` — remove linha com validação por old string
  - [ ] `file status` — preview das mudanças staged como unified diff
  - [ ] `file commit` — aplica todas as mudanças atomicamente com backup
  - [ ] `file rollback` — restaura arquivos do último backup
  - [ ] `file clear` — descarta todos os planos staged
- [ ] **Grupo `dotnet`**: Mutações AST-aware via Roslyn SyntaxRewriter
  - [ ] `dotnet scaffold class/interface/record/enum` — cria arquivo com boilerplate mínimo
  - [ ] `dotnet add method` — insere método na classe (bottom, before closing })
  - [ ] `dotnet add property` — insere propriedade com getter/setter gerados
  - [ ] `dotnet add field` — insere campo com prefixo underscore
  - [ ] `dotnet add constructor` — insere construtor na posição correta
  - [ ] `dotnet add using` — adiciona using directive se não presente
  - [ ] `dotnet update property/field` — atualiza membros existentes
  - [ ] `dotnet remove method/property/field` — remove membros pelo nome
- [ ] Persistência de estado dos planos em `.roslyn-nav-plans.json`
- [ ] Backup automático em `.roslyn-nav-backup/<timestamp>/` no commit
- [ ] Atualização do CLAUDE.md com os novos comandos, workflows e dicas

### Out of Scope

- Geração de código baseada em IA (a ferramenta é a camada de execução, não de decisão)
- Suporte a outras linguagens além de C#
- Watcher de arquivos em tempo real
- Interface visual ou servidor HTTP
- Upgrade de pacotes NuGet existentes
- Alteração dos comandos de navegação existentes

## Context

O projeto já tem 17 comandos de navegação implementados em .NET 10 usando `Microsoft.CodeAnalysis.CSharp`, `Microsoft.CodeAnalysis.Workspaces.MSBuild` e `System.CommandLine`. A estrutura é clara: `Commands/`, `Services/`, `Models/`. Os comandos write/edit usarão o padrão plan/commit com `IPlanStore`/`FilePlanStore`.

O principal desafio técnico é o `dotnet add method` — detectar indentação existente, lidar com namespaces file-scoped vs block-scoped, e manter a ordem convencional de membros C# (fields → properties → constructors → methods).

## Constraints

- **Tech stack**: .NET 10, C# — sem criar nova solution
- **Compatibilidade**: Pacotes NuGet existentes não mudam
- **Padrão existente**: Saída JSON em todos os comandos; seguir estrutura de `Commands/` existente
- **Atomicidade**: `file commit` aplica todas as mudanças ou nenhuma; backup sempre criado

## Key Decisions

| Decision | Rationale | Outcome |
|----------|-----------|---------|
| Plan/commit pattern (Unit of Work) | Edições individuais são frágeis; commit atômico garante consistência | — Pending |
| Validação linha + old string | Evita ambiguidade quando a mesma string aparece múltiplas vezes | — Pending |
| Grupos `file` e `dotnet` compartilham o mesmo estado de planos | Permite misturar edições raw e AST-aware no mesmo commit | — Pending |
| `IPlanStore` + `FilePlanStore` | Testabilidade + persistência entre invocações | — Pending |
| Namespace file-scoped por padrão no scaffold | C# 10+ padrão moderno | — Pending |

---
*Last updated: 2026-02-27 after initialization*
