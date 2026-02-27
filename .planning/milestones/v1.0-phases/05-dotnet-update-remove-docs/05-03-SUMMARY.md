---
phase: 05-dotnet-update-remove-docs
plan: 03
subsystem: cli
tags: [dotnet-tool, system-commandline, dotnet-update, dotnet-remove]

# Dependency graph
requires:
  - phase: 05-02
    provides: DotnetUpdateCommand and DotnetRemoveCommand service implementations
provides:
  - dotnet update property and dotnet update field CLI subcommands wired in Program.cs
  - dotnet remove method, remove property, and remove field CLI subcommands wired in Program.cs
  - Full end-to-end update/remove pipeline accessible via roslyn-nav dotnet update/remove
affects: []

# Tech tracking
tech-stack:
  added: []
  patterns:
    - Positional Argument<string> variables with unique local names per subcommand to avoid scope conflicts
    - Delegate to service static ExecuteAsync with memberKind string ("property", "field", "method")

key-files:
  created: []
  modified:
    - src/RoslynNavigator/Program.cs

key-decisions:
  - "dotnet update and dotnet remove groups registered after dotnetAddCommand in the dotnet command group, following the same positional-arg pattern"

patterns-established:
  - "Each subcommand block: declare args, create Command, add args, SetHandler with try/catch OutputError fallback"

requirements-completed: [DUPD-01, DUPD-02, DREM-01, DREM-02, DREM-03]

# Metrics
duration: 3min
completed: 2026-02-27
---

# Phase 5 Plan 03: dotnet update and dotnet remove CLI wiring Summary

**Five leaf CLI commands wired in Program.cs: `dotnet update` (property, field) and `dotnet remove` (method, property, field) delegating to DotnetUpdateCommand/DotnetRemoveCommand**

## Performance

- **Duration:** 3 min
- **Started:** 2026-02-27T11:49:26Z
- **Completed:** 2026-02-27T11:52:00Z
- **Tasks:** 1
- **Files modified:** 1

## Accomplishments
- Registered `dotnet update property` and `dotnet update field` subcommands delegating to `DotnetUpdateCommand.ExecuteAsync`
- Registered `dotnet remove method`, `remove property`, and `remove field` subcommands delegating to `DotnetRemoveCommand.ExecuteAsync`
- End-to-end smoke test confirmed: update property changes `{ get; set; }` to `{ get; init; }`, remove method eliminates the target method after `file commit`
- All 42 existing tests continue to pass

## Task Commits

Each task was committed atomically:

1. **Task 1: Register dotnet update and dotnet remove subcommand groups in Program.cs** - `9ae8c5b` (feat)

**Plan metadata:** _(docs commit follows)_

## Files Created/Modified
- `src/RoslynNavigator/Program.cs` - Added 126 lines: dotnet update property, dotnet update field, dotnet remove method, dotnet remove property, dotnet remove field subcommands plus group registrations

## Decisions Made
- Followed the exact positional-Argument pattern established in dotnet add commands (unique variable names per subcommand to avoid C# scope conflicts in top-level statements)
- dotnetUpdateCommand and dotnetRemoveCommand registered on dotnetCommand after dotnetAddCommand, before the file command group separator

## Deviations from Plan
None - plan executed exactly as written.

## Issues Encountered
None.

## User Setup Required
None - no external service configuration required.

## Next Phase Readiness
- Phase 5 plan 03 complete: update/remove pipeline fully accessible via CLI
- Phase 5 plan 04 (CLAUDE.md documentation) was already completed — entire Phase 5 is now done

## Self-Check: PASSED
- FOUND: 05-03-SUMMARY.md
- FOUND: Program.cs
- FOUND commit: 9ae8c5b

---
*Phase: 05-dotnet-update-remove-docs*
*Completed: 2026-02-27*
