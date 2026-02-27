# Project State

## Project Reference

See: .planning/PROJECT.md (updated 2026-02-27)

**Core value:** O assistente de IA consegue navegar, criar e modificar código C# com precisão cirúrgica sem precisar ler arquivos inteiros — reduzindo tokens e eliminando edições ambíguas.
**Current focus:** Milestone v2.0 Snapshot, Rules & Ask

## Current Position

Phase: 2 (Rules Engine)
Plan: 3 (completed)
Status: Plan 02-03 complete - Check command implemented
Last activity: 2026-02-27 — Completed plan 02-03 (Check command CLI integration)

Progress: [▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓] 67%

## Current Milestone

**v2.0 Snapshot, Rules & Ask** (Phases 1-4)

- Phase 1: Snapshot Foundation — Generate SQLite snapshot with full schema
- Phase 2: Rules Engine — Evaluate YAML rules and report violations
- Phase 3: Query Integration — SQL arbitrary queries with JSON output
- Phase 4: Integration & Polish — New projects, CLI integration, compatibility

## Accumulated Context

### Decisions

- **01-01 Completed:** Snapshot Foundation - SQLite with embedded schema, schema/path services, 18 tests pass
- **01-02 Completed:** Snapshot Extraction - SnapshotExtractorService, SnapshotSignalAnalyzer, 9 TDD tests, 69 tests total pass
- **01-03 Completed:** Wire snapshot to CLI - SnapshotCommand, CLI registration, 9 command tests, tests pass
- **02-01 Completed:** Rule Loader Foundation - YamlDotNet, embedded YAML packs, RuleLoaderService, 16 loader tests
- **02-02 Completed:** Rule SQL Compilation - RuleSqlCompiler with LIKE wildcards, RuleEvaluatorService with read-only queries, NOT EXISTS semantics, 19 tests
- **02-03 Completed:** Check Command - CLI command with --db, --severity, --ruleId filters, 10 command tests
See PROJECT.md for key decisions from v1.0.

### Pending Todos

- Phase 2: Complete Rules Engine (RULE-01, RULE-05 now complete with 02-03)
- Phase 3: Implement Query Integration (ASK-01 through ASK-03)
- Phase 4: Integration & Polish (INT-01 through INT-03)

### Blockers/Concerns

None yet.

## Session Continuity

Last session: 2026-02-27
Stopped at: Completed plan 02-03 - Check command CLI integration
Resume from: Phase 2 complete - ready for Phase 3 (Query Integration)

---

## v2.0 Requirements Summary

| Category | Requirements | Phase |
|----------|--------------|-------|
| Snapshot | SNAP-01, SNAP-02, SNAP-03, SNAP-04 | 1 |
| Rules | RULE-01, RULE-02, RULE-03, RULE-04, RULE-05 | 2 |
| Query | ASK-01, ASK-02, ASK-03 | 3 |
| Integration | INT-01, INT-02, INT-03 | 4 |

**Total:** 15 requirements across 4 phases
