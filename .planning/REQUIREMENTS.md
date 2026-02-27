# Requirements: RoslynNavigator — File & Dotnet Commands

**Defined:** 2026-02-27
**Core Value:** O assistente de IA consegue navegar, criar e modificar código C# com precisão cirúrgica sem precisar ler arquivos inteiros — reduzindo tokens e eliminando edições ambíguas.

## v1 Requirements

### Infrastructure

- [x] **INFRA-01**: Sistema de plan/commit (Unit of Work) — planos são staged em memória e `.roslyn-nav-plans.json` persiste entre invocações
- [x] **INFRA-02**: `IPlanStore` interface com `FilePlanStore` implementation que lê/escreve `.roslyn-nav-plans.json` no diretório de trabalho
- [x] **INFRA-03**: Backup automático em `.roslyn-nav-backup/<timestamp>/` criado antes de qualquer `file commit`
- [x] **INFRA-04**: Atomicidade — `file commit` aplica todas as mudanças ou nenhuma; se qualquer validação falhar, nenhum arquivo é tocado

### File Read

- [x] **FREAD-01**: `file read <path> [--lines START-END]` — exibe conteúdo do arquivo sempre com números de linha; aceita range opcional
- [x] **FREAD-02**: `file grep <pattern> [path] [--ext .cs] [--max-lines 100]` — busca regex com filtro de extensão e limite de resultados

### File Stage (Write)

- [x] **FSTAGE-01**: `file plan edit <path> <line> <old> <new>` — edição determinística: valida que a linha `<line>` contém `<old>` antes de aceitar; recusa se não bater
- [x] **FSTAGE-02**: `file plan write <path> <content>` — staged: cria ou sobrescreve o arquivo inteiro com `<content>`
- [x] **FSTAGE-03**: `file plan append <path> <content>` — staged: adiciona `<content>` ao final do arquivo
- [x] **FSTAGE-04**: `file plan delete <path> <line> <old>` — staged: remove a linha `<line>`, validando que contém `<old>`

### File Commit / Rollback

- [x] **FCOMMIT-01**: `file status` — exibe todas as mudanças staged como unified diff preview; aceita `--json` para saída machine-readable
- [x] **FCOMMIT-02**: `file commit` — cria backup, valida todas as operações (falha rápido se qualquer validação falhar), aplica atomicamente, retorna unified diff; aceita `--json`
- [x] **FCOMMIT-03**: `file rollback` — restaura todos os arquivos modificados do último backup em `.roslyn-nav-backup/`
- [x] **FCOMMIT-04**: `file clear` — descarta todos os planos staged sem aplicar, deleta `.roslyn-nav-plans.json`

### Dotnet Scaffold

- [x] **SCAF-01**: `dotnet scaffold class <path> <namespace> <className>` — gera arquivo com `namespace <ns>; public class <name> { }` (file-scoped namespace)
- [x] **SCAF-02**: `dotnet scaffold interface <path> <namespace> <interfaceName>` — gera `public interface <name> { }`
- [x] **SCAF-03**: `dotnet scaffold record <path> <namespace> <recordName>` — gera `public record <name> { }`
- [x] **SCAF-04**: `dotnet scaffold enum <path> <namespace> <enumName>` — gera `public enum <name> { }`

### Dotnet Add

- [x] **DADD-01**: `dotnet add using <path> <namespace>` — adiciona `using <namespace>;` no topo do arquivo se não estiver presente
- [x] **DADD-02**: `dotnet add field <path> <className> <access> <type> <name>` — insere `<access> <type> _<name>;` na posição correta (top of class body, after last field)
- [x] **DADD-03**: `dotnet add property <path> <className> <access> <type> <name>` — insere `<access> <type> <Name> { get; set; }` na posição correta (after fields)
- [x] **DADD-04**: `dotnet add constructor <path> <className> <content>` — insere construtor na posição correta (after properties/fields)
- [x] **DADD-05**: `dotnet add method <path> <className> <content>` — insere método antes do `}` de fechamento da classe; detecta indentação existente; valida que o conteúdo parsa sem erros
- [x] **DADD-06**: Ordem convencional de membros C# respeitada em todos os `dotnet add`: fields → properties → constructors → methods

### Dotnet Update & Remove

- [x] **DUPD-01**: `dotnet update property <path> <className> <propertyName> <content>` — substitui a propriedade existente pelo `<content>` fornecido
- [x] **DUPD-02**: `dotnet update field <path> <className> <fieldName> <content>` — substitui o campo existente pelo `<content>` fornecido
- [x] **DREM-01**: `dotnet remove method <path> <className> <methodName>` — remove método pelo nome
- [x] **DREM-02**: `dotnet remove property <path> <className> <propertyName>` — remove propriedade pelo nome
- [x] **DREM-03**: `dotnet remove field <path> <className> <fieldName>` — remove campo pelo nome

### Cross-cutting

- [x] **CROSS-01**: Todos os comandos `dotnet` write/edit são staged (compartilham o mesmo `IPlanStore` dos comandos `file`)
- [x] **CROSS-02**: Mensagens de erro específicas: linha errada, old string não encontrada, classe não encontrada, erro de parse no conteúdo
- [x] **CROSS-03**: `dotnet add`, `update` e `remove` resolvem o tipo alvo buscando em `ClassDeclarationSyntax`, `RecordDeclarationSyntax` e `StructDeclarationSyntax`
- [ ] **CROSS-04**: CLAUDE.md atualizado com todos os novos comandos, workflows e dicas

## v2 Requirements

### Dotnet Advanced

- **DADV-01**: `dotnet add interface-implementation <path> <className> <interfaceName>` — adiciona interface ao tipo e gera stubs dos métodos
- **DADV-02**: `dotnet scaffold controller <path> <namespace> <name>` — boilerplate de controller ASP.NET com atributos
- **DADV-03**: Suporte a namespaces block-scoped (além do file-scoped já suportado no scaffold)

### File Advanced

- **FADV-01**: `file diff` — compara estado atual com o staged antes do commit
- **FADV-02**: `file plan move <src> <dst>` — move/renomeia arquivo

## Out of Scope

| Feature | Reason |
|---------|--------|
| Geração de código por IA | A ferramenta executa; a IA decide — sem overlap |
| Suporte a VB.NET ou F# | C# only, mesma decisão do projeto base |
| Servidor HTTP / daemon | CLI one-shot, mesma decisão do projeto base |
| Upgrade de pacotes NuGet | Estabilidade da ferramenta existente |
| Modificação de comandos de navegação existentes | Não quebrar o que já funciona |

## Traceability

| Requirement | Phase | Status |
|-------------|-------|--------|
| INFRA-01 | Phase 1 | Complete |
| INFRA-02 | Phase 1 | Complete |
| INFRA-03 | Phase 1 | Complete |
| INFRA-04 | Phase 1 | Complete |
| FREAD-01 | Phase 1 | Complete |
| FREAD-02 | Phase 1 | Complete |
| FSTAGE-01 | Phase 2 | Complete |
| FSTAGE-02 | Phase 2 | Complete |
| FSTAGE-03 | Phase 2 | Complete |
| FSTAGE-04 | Phase 2 | Complete |
| FCOMMIT-01 | Phase 2 | Complete |
| FCOMMIT-02 | Phase 2 | Complete |
| FCOMMIT-03 | Phase 2 | Complete |
| FCOMMIT-04 | Phase 2 | Complete |
| SCAF-01 | Phase 3 | Complete |
| SCAF-02 | Phase 3 | Complete |
| SCAF-03 | Phase 3 | Complete |
| SCAF-04 | Phase 3 | Complete |
| DADD-01 | Phase 4 | Complete |
| DADD-02 | Phase 4 | Complete |
| DADD-03 | Phase 4 | Complete |
| DADD-04 | Phase 4 | Complete |
| DADD-05 | Phase 4 | Complete |
| DADD-06 | Phase 4 | Complete |
| DUPD-01 | Phase 5 | Complete |
| DUPD-02 | Phase 5 | Complete |
| DREM-01 | Phase 5 | Complete |
| DREM-02 | Phase 5 | Complete |
| DREM-03 | Phase 5 | Complete |
| CROSS-01 | Phase 1 | Complete |
| CROSS-02 | Phase 2 | Complete |
| CROSS-03 | Phase 4 | Complete |
| CROSS-04 | Phase 5 | Pending |

**Coverage:**
- v1 requirements: 32 total
- Mapped to phases: 32
- Unmapped: 0 ✓

---
*Requirements defined: 2026-02-27*
*Last updated: 2026-02-27 after initial definition*
