# Milestones

## v1.0 File & Dotnet Commands (Shipped: 2026-02-27)

**Phases completed:** 5 phases, 13 plans
**Files modified:** 122 | **LOC C#:** ~7.165 | **Timeline:** 2026-01-16 → 2026-02-27 (42 dias)
**Requirements:** 32/32 v1 requirements Complete

**Delivered:** Capacidade completa de leitura e mutação de arquivos C# para assistentes de IA — plan/commit atômico, CRUD raw de arquivos, e mutações AST-aware via Roslyn (scaffold, add, update, remove de membros).

**Key accomplishments:**
- Plan/commit Unit of Work (IPlanStore, FilePlanStore, BackupService) — staging seguro de operações de arquivo com persistência em `.roslyn-nav-plans.json` e backup automático antes de cada commit
- `file read` / `file grep` — leitura imediata com line numbers e busca regex com filtro de extensão
- `file plan edit/write/append/delete` + `file status/commit/rollback/clear` — CRUD atômico de arquivos com diff preview unificado e rollback garantido
- `dotnet scaffold class/interface/record/enum` — geração de arquivos C# com namespace file-scoped e boilerplate mínimo
- `dotnet add field/property/constructor/method/using` — inserção AST-aware via Roslyn respeitando ordem convencional C# (fields → properties → constructors → methods)
- `dotnet update property/field` + `dotnet remove method/property/field` — mutação e remoção de membros existentes; CLAUDE.md documentado com todos os 17+ novos comandos

**Archives:**
- `.planning/milestones/v1.0-ROADMAP.md`
- `.planning/milestones/v1.0-REQUIREMENTS.md`

---


## v2.1 Sonar Baseline Scope (Shipped: 2026-02-27)

**Phases completed:** Phases 1-4 (10 plans)
**Files modified:** ~87 | **LOC C#:** ~13.229 | **Timeline:** 2026-02-27 (single session)
**Requirements:** 15/15 v2.1 requirements complete

**Delivered:** Snapshot SQLite da solution + rules engine YAML + query SQL direta — `roslyn-nav` agora analisa codebases inteiras sem Roslyn em runtime, exportando estrutura completa para consulta por LLM.

**Key accomplishments:**
- `roslyn-nav snapshot` — gera banco SQLite com tabelas `classes`, `methods`, `dependencies`, `calls`, `annotations`, `flags`, `snapshot_meta`; inclui sinais de análise (`returns_null`, `cognitive_complexity`, `has_try_catch`, `calls_external`, `accesses_db`, `filters_by_tenant`)
- `roslyn-nav check` — avalia regras YAML builtin/domain (`architecture.yaml`, `code-quality.yaml`, `security.yaml`) e reporta violações filtráveis por `--severity` e `--ruleId`; suporta wildcards (`IRepo.*`) e bloco `not:` com semântica NOT EXISTS
- `roslyn-nav snapshot query` — executa SQL arbitrário no snapshot com output JSON consistente (`rows: List<Dictionary>`) para consumo por LLM externa; read-only guard (SqlReadOnlyGuard) impede mutação
- `RoslynNavigator.Snapshot` + `RoslynNavigator.Rules` — solution refatorada em 4 projetos (CLI, Snapshot lib, Rules lib, Tests); 201 testes passando; todos os 41 comandos anteriores mantidos sem regressão
- Integração de baseline Sonar C# — catálogo `SonarQube.yaml` com mapeamentos de alta confiança; `check --rules` aceita arquivos customizados; sinais de snapshot expandidos para suportar regras de segurança

**Stats:**
- 4 phases, 10 plans, 35 tasks
- ~87 files changed, ~8.957 insertions
- 13.229 LOC C# total (solution)
- Git range: `feat(01-01)` → `feat(04-01)` + `a9df96d`

**What's next:** Planning next milestone — possíveis direções: suporte a projetos C++ via Clang, integração nativa com IDEs via LSP, ou expansão do rules engine para regras customizadas via UI.

**Archives:**
- `.planning/milestones/v2.1-ROADMAP.md`
- `.planning/milestones/v2.1-REQUIREMENTS.md`

---

