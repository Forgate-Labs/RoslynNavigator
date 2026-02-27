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

