# Phase 02 Discovery: Rules Engine

## Discovery Level

Level 2 (standard research) due to new external dependency (`YamlDotNet`) and rule-engine semantics (`LIKE` wildcard + `NOT EXISTS`) that must match roadmap requirements.

## Inputs Reviewed

- `.planning/ROADMAP.md` (Phase 2 goal and RULE-01..RULE-05)
- `.planning/REQUIREMENTS.md` (requirement wording)
- `.planning/STATE.md` (current position: Phase 1 complete)
- `.planning/phases/01-snapshot-foundation/*-SUMMARY.md` (snapshot and CLI patterns already shipped)
- `src/RoslynNavigator/RoslynNavigator.csproj` (current package set)
- `src/RoslynNavigator/Program.cs` (command registration and JSON error/output contract)
- `src/RoslynNavigator/Commands/SnapshotCommand.cs` (command orchestration pattern)

## Key Findings

1. No YAML parser exists in current dependencies; `YamlDotNet` is required to implement RULE-02.
2. Phase 1 already provides the SQLite snapshot schema (`classes`, `methods`, `dependencies`, `calls`, `annotations`, `flags`, `snapshot_meta`) and command result/error JSON conventions.
3. Existing CLI commands follow a stable pattern:
   - small command class orchestrates services
   - `Program.cs` wires options and catches exceptions via `OutputError`
4. RULE-03 and RULE-04 are easiest to stabilize with TDD around SQL generation/evaluation:
   - wildcard `IRepo.*` should compile to SQL `LIKE`
   - `not:` block should compile to `NOT EXISTS (...)`

## Planning Decisions

- Use `YamlDotNet` (locked in plan actions) for YAML parsing.
- Add builtin rule packs as embedded resources to match prior embedded-resource pattern used by `SnapshotSchema.sql`.
- Keep evaluation read-only (`SELECT` queries only), preserving snapshot immutability and phase compatibility.
- Split implementation into 3 plans:
  1) rule contracts + loader + builtin resources,
  2) TDD SQL compiler/evaluator semantics,
  3) `check` command wiring + filters + end-to-end tests.
