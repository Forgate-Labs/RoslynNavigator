---
phase: 02-file-stage-commit
plan: 02
subsystem: cli
tags: [system.commandline, staging, file-plan, unified-diff]

requires:
  - phase: 02-file-stage-commit/02-01
    provides: FilePlanEngine (ValidateAsync, ComputeDiff), FilePlanStore, IPlanStore, PlanModels

provides:
  - FilePlanEditCommand — stages Edit ops with eager validation via FilePlanEngine.ValidateAsync
  - FilePlanDeleteCommand — stages Delete ops with eager validation via FilePlanEngine.ValidateAsync
  - FilePlanWriteCommand — stages Write ops without validation
  - FilePlanAppendCommand — stages Append ops without validation
  - FileStatusCommand — renders unified diff of all staged ops without touching files
  - FileStagingResults.cs — FilePlanStagedResult and FileStatusResult JSON result models
  - Program.cs wired: file plan (edit/write/append/delete) + file status subcommands

affects: [02-03-file-commit-rollback]

tech-stack:
  added: []
  patterns:
    - Static command class pattern: each command is a static class with ExecuteAsync, no coupling to CLI parsing
    - Eager validation pattern: Edit and Delete commands validate before staging; Write/Append skip validation
    - Plan store pattern: all staging commands load-mutate-save via FilePlanStore.CreateDefault()

key-files:
  created:
    - src/RoslynNavigator/Models/FileStagingResults.cs
    - src/RoslynNavigator/Commands/FilePlanEditCommand.cs
    - src/RoslynNavigator/Commands/FilePlanDeleteCommand.cs
    - src/RoslynNavigator/Commands/FilePlanWriteCommand.cs
    - src/RoslynNavigator/Commands/FilePlanAppendCommand.cs
    - src/RoslynNavigator/Commands/FileStatusCommand.cs
  modified:
    - src/RoslynNavigator/Program.cs

key-decisions:
  - "Edit and Delete validate eagerly via FilePlanEngine.ValidateAsync([op], cwd) before staging; first error wins"
  - "Write and Append skip validation entirely — always accepted per FSTAGE-02/03"
  - "FileStatusCommand returns UnifiedDiff='(no staged operations)' string when no ops staged — consistent with file status text output"
  - "file status --json flag outputs full FileStatusResult JSON; without flag outputs only UnifiedDiff string for human readability"

patterns-established:
  - "Staging pattern: load plan state, add op, save plan state — all via FilePlanStore.CreateDefault()"
  - "CLI subgroup pattern: filePlanCommand under fileCommand, subcommands under filePlanCommand"

requirements-completed: [FSTAGE-01, FSTAGE-02, FSTAGE-03, FSTAGE-04, FCOMMIT-01, CROSS-02]

duration: 2min
completed: 2026-02-27
---

# Phase 02 Plan 02: File Stage Commands Summary

**Five staging commands (file plan edit/write/append/delete + file status) wired into CLI with eager validation using FilePlanEngine and unified diff preview via FileStatusCommand**

## Performance

- **Duration:** 2 min
- **Started:** 2026-02-27T21:03:28Z
- **Completed:** 2026-02-27T21:05:20Z
- **Tasks:** 3
- **Files modified:** 7

## Accomplishments

- Four `file plan` subcommands (edit, write, append, delete) staged as PlanOperations persisted to `.roslyn-nav-plans.json`
- Edit and Delete validate eagerly via `FilePlanEngine.ValidateAsync` before accepting — rejects if line/old mismatch
- `file status` renders a unified diff of all staged ops without touching files; returns empty message when no ops staged
- All commands registered under `file plan` subgroup and `file status` in Program.cs
- All 13 tests pass; 0 build errors

## Task Commits

1. **Task 1: Result models and FilePlanEdit/DeleteCommand** - `98a8d85` (feat)
2. **Task 2: FilePlanWrite/AppendCommand and FileStatusCommand** - `52e40e8` (feat)
3. **Task 3: Register file plan subgroup and file status in Program.cs** - `5834eb7` (feat)

## Files Created/Modified

- `src/RoslynNavigator/Models/FileStagingResults.cs` - FilePlanStagedResult and FileStatusResult records
- `src/RoslynNavigator/Commands/FilePlanEditCommand.cs` - Stages Edit op with ValidateAsync guard
- `src/RoslynNavigator/Commands/FilePlanDeleteCommand.cs` - Stages Delete op with ValidateAsync guard
- `src/RoslynNavigator/Commands/FilePlanWriteCommand.cs` - Stages Write op (no validation)
- `src/RoslynNavigator/Commands/FilePlanAppendCommand.cs` - Stages Append op (no validation)
- `src/RoslynNavigator/Commands/FileStatusCommand.cs` - Computes and returns unified diff via FilePlanEngine.ComputeDiff
- `src/RoslynNavigator/Program.cs` - Registered file plan subgroup and file status subcommand under file command

## Decisions Made

- Edit and Delete validate eagerly using `FilePlanEngine.ValidateAsync([op], cwd)` — first error wins and throws `InvalidOperationException`
- Write and Append skip validation entirely per plan spec (always accepted)
- `file status` without `--json` outputs only the UnifiedDiff string for human readability; with `--json` outputs full FileStatusResult
- `FileStatusCommand` returns `"(no staged operations)"` as the UnifiedDiff string when the plan is empty — consistent messaging

## Deviations from Plan

None - plan executed exactly as written.

## Issues Encountered

None.

## User Setup Required

None - no external service configuration required.

## Next Phase Readiness

- Staging layer complete: Edit, Delete (validated), Write, Append, and status preview are fully working
- Ready for Plan 03: file commit and rollback commands that call `FilePlanEngine.CommitAsync` and `FilePlanEngine.RollbackAsync`

---
*Phase: 02-file-stage-commit*
*Completed: 2026-02-27*

## Self-Check: PASSED

- All 6 new files exist on disk
- All 3 task commits found: 98a8d85, 52e40e8, 5834eb7
