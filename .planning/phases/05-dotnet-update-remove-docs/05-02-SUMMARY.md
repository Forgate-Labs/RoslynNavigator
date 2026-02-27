---
phase: 05-dotnet-update-remove-docs
plan: 02
subsystem: api
tags: [roslyn, csharp, plan-engine, staging, update, remove]

# Dependency graph
requires:
  - phase: 05-01
    provides: DotnetUpdateRemoveService (UpdateMember, RemoveMember)
  - phase: 04-02
    provides: FilePlanEngine AddMember pattern, DotnetAddCommand pattern
provides:
  - UpdateMember and RemoveMember OperationType enum values
  - DotnetUpdateResult and DotnetRemoveResult records
  - DotnetUpdateCommand.ExecuteAsync staging method
  - DotnetRemoveCommand.ExecuteAsync staging method
  - FilePlanEngine.ValidateAsync cases for UpdateMember and RemoveMember
  - FilePlanEngine.ApplyOpsInMemory cases for UpdateMember and RemoveMember
affects: [05-03-CLI-wiring]

# Tech tracking
tech-stack:
  added: []
  patterns:
    - "ParseUpdateRemoveMetadata: camelCase JSON with typeName, memberKind, memberName, content fields"
    - "UpdateMember/RemoveMember ops follow same dry-run validate + apply-in-memory pattern as AddMember"
    - "JsonSerializer.Deserialize<JsonElement> with TryGetProperty for optional content field (RemoveMember has no content)"

key-files:
  created:
    - src/RoslynNavigator/Models/DotnetUpdateRemoveResults.cs
    - src/RoslynNavigator/Commands/DotnetUpdateCommand.cs
    - src/RoslynNavigator/Commands/DotnetRemoveCommand.cs
  modified:
    - src/RoslynNavigator/Models/PlanModels.cs
    - src/RoslynNavigator/Services/FilePlanEngine.cs

key-decisions:
  - "ParseUpdateRemoveMetadata uses TryGetProperty for content to handle RemoveMember ops that have no content field"
  - "UpdateMember and RemoveMember follow exact same validate/apply pattern as AddMember in FilePlanEngine"
  - "Metadata camelCase JSON: typeName, memberKind, memberName, content (content absent for RemoveMember)"

requirements-completed: [DUPD-01, DUPD-02, DREM-01, DREM-02, DREM-03]

# Metrics
duration: 1min
completed: 2026-02-27
---

# Phase 5 Plan 02: DotnetUpdateRemoveService Wiring Summary

**UpdateMember and RemoveMember op types wired into the plan/commit pipeline: OperationType enum, result models, staging command classes, and FilePlanEngine validate/apply cases**

## Performance

- **Duration:** 1 min
- **Started:** 2026-02-27T11:46:21Z
- **Completed:** 2026-02-27T11:47:00Z
- **Tasks:** 2
- **Files modified:** 5

## Accomplishments

- Added `UpdateMember` and `RemoveMember` to the `OperationType` enum in `PlanModels.cs`
- Created `DotnetUpdateRemoveResults.cs` with `DotnetUpdateResult` and `DotnetRemoveResult` records
- Created `DotnetUpdateCommand.cs` — stages `UpdateMember` ops with camelCase JSON metadata: `{ typeName, memberKind, memberName, content }`
- Created `DotnetRemoveCommand.cs` — stages `RemoveMember` ops with camelCase JSON metadata: `{ typeName, memberKind, memberName }`
- Extended `FilePlanEngine.ValidateAsync` with dry-run cases for both op types via `DotnetUpdateRemoveService`
- Extended `FilePlanEngine.ApplyOpsInMemory` with apply cases for both op types using `ModifiedSource`
- Added `ParseUpdateRemoveMetadata` private helper with `TryGetProperty` for optional `content` field
- All 42 tests pass; build succeeds with zero errors

## Task Commits

1. **Task 1: OperationType enum + result models + staging commands** - `5792f38` (feat)
2. **Task 2: FilePlanEngine ValidateAsync and ApplyOpsInMemory** - `843e15f` (feat)

## Files Created/Modified

- `src/RoslynNavigator/Models/PlanModels.cs` — Added `UpdateMember`, `RemoveMember` enum values
- `src/RoslynNavigator/Models/DotnetUpdateRemoveResults.cs` — New: `DotnetUpdateResult` and `DotnetRemoveResult` records
- `src/RoslynNavigator/Commands/DotnetUpdateCommand.cs` — New: staging command for update-property/update-field
- `src/RoslynNavigator/Commands/DotnetRemoveCommand.cs` — New: staging command for remove-method/remove-property/remove-field
- `src/RoslynNavigator/Services/FilePlanEngine.cs` — ValidateAsync + ApplyOpsInMemory cases for UpdateMember/RemoveMember

## Decisions Made

- `ParseUpdateRemoveMetadata` uses `TryGetProperty` for `content` so the same helper works for both UpdateMember (has content) and RemoveMember (no content field)
- Both ops follow the existing AddMember pattern exactly: dry-run in ValidateAsync, apply-in-memory in ApplyOpsInMemory
- Metadata uses camelCase JSON consistent with `JsonNamingPolicy.CamelCase` policy used across the codebase

## Deviations from Plan

None - plan executed exactly as written.

## Issues Encountered

None.

## User Setup Required

None - no external service configuration required.

## Next Phase Readiness

- `DotnetUpdateCommand` and `DotnetRemoveCommand` are ready for CLI wiring in plan 05-03
- `FilePlanEngine` handles all 6 operation types: Edit, Write, Append, Delete, ScaffoldFile, AddMember, UpdateMember, RemoveMember
- No blockers

---
*Phase: 05-dotnet-update-remove-docs*
*Completed: 2026-02-27*

## Self-Check: PASSED

- FOUND: src/RoslynNavigator/Models/PlanModels.cs (UpdateMember + RemoveMember in enum)
- FOUND: src/RoslynNavigator/Models/DotnetUpdateRemoveResults.cs
- FOUND: src/RoslynNavigator/Commands/DotnetUpdateCommand.cs
- FOUND: src/RoslynNavigator/Commands/DotnetRemoveCommand.cs
- FOUND: src/RoslynNavigator/Services/FilePlanEngine.cs (ParseUpdateRemoveMetadata + 4 new cases)
- FOUND commit: 5792f38 (Task 1)
- FOUND commit: 843e15f (Task 2)
