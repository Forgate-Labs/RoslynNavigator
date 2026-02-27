---
phase: 04-dotnet-add
plan: 02
subsystem: api
tags: [roslyn, csharp, staging, cli-command, file-plan-engine]

# Dependency graph
requires:
  - phase: 04-dotnet-add
    plan: 01
    provides: DotnetAddMemberService.AddMember and AddUsing
  - phase: 02-file-stage-commit
    provides: FilePlanEngine validate/apply/commit pipeline
provides:
  - DotnetAddResult record for all add operations
  - DotnetAddCommand.ExecuteMemberAsync — stages AddMember op for field/property/constructor/method
  - DotnetAddCommand.ExecuteUsingAsync — stages AddMember op with memberKind='using'
  - FilePlanEngine.ValidateAsync AddMember case — validates via DotnetAddMemberService dry-run
  - FilePlanEngine.ApplyOpsInMemory AddMember case — applies via DotnetAddMemberService
affects: [04-dotnet-add plan 03+, Program.cs CLI wiring for dotnet add commands]

# Tech tracking
tech-stack:
  added: []
  patterns:
    - "Metadata JSON (camelCase): { typeName, memberKind, content } passed through staging op to engine"
    - "ParseAddMemberMetadata private helper centralizes JSON parsing for AddMember ops"
    - "ValidateAsync: dry-run DotnetAddMemberService before any write to collect errors early"
    - "ApplyOpsInMemory: join lines → call service → split result back to lines (same pattern as Write/ScaffoldFile)"

key-files:
  created:
    - src/RoslynNavigator/Models/DotnetAddResults.cs
    - src/RoslynNavigator/Commands/DotnetAddCommand.cs
  modified:
    - src/RoslynNavigator/Services/FilePlanEngine.cs

key-decisions:
  - "Metadata camelCase JSON { typeName, memberKind, content } passed as PlanOperation.Metadata — consistent with JsonNamingPolicy.CamelCase used across API serialization"
  - "Using memberKind='using' with typeName='' as convention for using directive ops, matching plan spec"
  - "ValidateAsync uses separate variable assignment for AddMember/AddUsing results instead of a shared base type — simpler with no new types required"

requirements-completed: [DADD-02, DADD-03, DADD-04, DADD-05, CROSS-03]

# Metrics
duration: 2min
completed: 2026-02-27
---

# Phase 4 Plan 2: DotnetAddCommand and FilePlanEngine AddMember Wiring Summary

**AddMember op type wired through FilePlanEngine validate/apply pipeline with DotnetAddCommand staging class and DotnetAddResult model — 29 tests passing**

## Performance

- **Duration:** 2 min
- **Started:** 2026-02-27T04:20:57Z
- **Completed:** 2026-02-27T04:22:11Z
- **Tasks:** 2
- **Files modified:** 3

## Accomplishments

- `DotnetAddResult` record provides uniform output for all five add operations (field/property/constructor/method/using)
- `DotnetAddCommand.ExecuteMemberAsync` stages `AddMember` ops with camelCase metadata JSON for field/property/constructor/method
- `DotnetAddCommand.ExecuteUsingAsync` stages `AddMember` ops with `memberKind="using"` and `typeName=""` for using directives
- `FilePlanEngine.ValidateAsync` now handles `AddMember` — reads the file, calls `DotnetAddMemberService` as a dry-run, accumulates errors
- `FilePlanEngine.ApplyOpsInMemory` now handles `AddMember` — joins lines to source, calls service, splits modified source back to lines
- `ParseAddMemberMetadata` private helper centralizes JSON parsing of `{ typeName, memberKind, content }`
- All 29 existing tests continue to pass with no regressions

## Task Commits

Each task was committed atomically:

1. **Task 1: DotnetAddResult model and DotnetAddCommand** - `a7f4fa4` (feat)
2. **Task 2: Extend FilePlanEngine for AddMember** - `191949f` (feat)

## Files Created/Modified

- `src/RoslynNavigator/Models/DotnetAddResults.cs` — DotnetAddResult record with Operation, FilePath, TypeName, MemberKind, TotalStagedOps
- `src/RoslynNavigator/Commands/DotnetAddCommand.cs` — Static class with ExecuteMemberAsync and ExecuteUsingAsync staging methods
- `src/RoslynNavigator/Services/FilePlanEngine.cs` — AddMember case in ValidateAsync, AddMember case in ApplyOpsInMemory, ParseAddMemberMetadata helper

## Decisions Made

- Metadata JSON serialized as camelCase `{ typeName, memberKind, content }` using `JsonNamingPolicy.CamelCase` — matches the project-wide API serialization policy
- `memberKind="using"` with `typeName=""` as the convention for using directive staging ops — clean and consistent
- `ValidateAsync` uses separate variables for `AddMemberResult` vs `AddUsingResult` rather than creating a shared base type — avoids unnecessary new types while remaining explicit

## Deviations from Plan

None - plan executed exactly as written.

## Issues Encountered

None.

## User Setup Required

None.

## Next Phase Readiness

- `DotnetAddCommand` and `FilePlanEngine` AddMember support are complete — ready for CLI wiring in plan 04-03
- Program.cs can register `dotnet add field/property/constructor/method/using` subcommands that delegate to `DotnetAddCommand`
- No blockers

## Self-Check: PASSED

- `src/RoslynNavigator/Models/DotnetAddResults.cs` — FOUND
- `src/RoslynNavigator/Commands/DotnetAddCommand.cs` — FOUND
- `.planning/phases/04-dotnet-add/04-02-SUMMARY.md` — FOUND
- Commit `a7f4fa4` (Task 1) — FOUND
- Commit `191949f` (Task 2) — FOUND

---
*Phase: 04-dotnet-add*
*Completed: 2026-02-27*
