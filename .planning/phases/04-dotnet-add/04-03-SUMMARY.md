---
phase: 04-dotnet-add
plan: 03
subsystem: cli
tags: [system.commandline, dotnet-add, roslyn, program.cs]

# Dependency graph
requires:
  - phase: 04-dotnet-add
    provides: DotnetAddCommand.ExecuteMemberAsync and ExecuteUsingAsync with full AddMember pipeline
provides:
  - CLI surface for dotnet add using/field/property/constructor/method subcommands
  - dotnetAddCommand group registered under dotnetCommand in Program.cs
affects: [05-dotnet-edit, future-dotnet-subgroups]

# Tech tracking
tech-stack:
  added: []
  patterns: [positional-argument-subcommand, content-building-in-handler, underscore-prefix-auto-added]

key-files:
  created: []
  modified:
    - src/RoslynNavigator/Program.cs

key-decisions:
  - "dotnet add subcommand uses positional Argument<string> variables with unique local names to avoid scope conflicts"
  - "Field command auto-prepends underscore to name arg — caller provides base name, handler builds _{name}"
  - "Property command auto-formats {access} {type} {name} {{ get; set; }} — caller provides PascalCase name"
  - "Constructor and method commands accept full source string as content arg — caller responsible for correct syntax"
  - "dotnetAddCommand registered after dotnetScaffoldCommand in dotnetCommand group"

patterns-established:
  - "Content-building pattern: handler constructs member content string before delegating to ExecuteMemberAsync"
  - "Error code naming convention: dotnet_add_{subcommand}_error mirrors dotnet_scaffold_{subcommand}_error"

requirements-completed: [DADD-01, DADD-02, DADD-03, DADD-04, DADD-05, DADD-06]

# Metrics
duration: 3min
completed: 2026-02-27
---

# Phase 4 Plan 03: dotnet add CLI wiring Summary

**CLI surface for `roslyn-nav dotnet add` with 5 leaf commands (using/field/property/constructor/method) wired to DotnetAddCommand in Program.cs**

## Performance

- **Duration:** 3 min
- **Started:** 2026-02-27T04:24:06Z
- **Completed:** 2026-02-27T04:27:00Z
- **Tasks:** 1
- **Files modified:** 1

## Accomplishments

- Registered `dotnet add` subcommand group under `dotnetCommand` in Program.cs
- Implemented all 5 leaf commands: using, field, property, constructor, method
- Field handler auto-builds `{access} {type} _{name};` content string
- Property handler auto-builds `{access} {type} {name} { get; set; }` content string
- End-to-end smoke test verified: `dotnet add field` stages, `file commit` writes `_name` field to disk
- All 29 existing tests continue to pass

## Task Commits

Each task was committed atomically:

1. **Task 1: Wire dotnet add subcommand group in Program.cs** - `026cf2e` (feat)

**Plan metadata:** (docs commit follows)

## Files Created/Modified

- `src/RoslynNavigator/Program.cs` - Added 126 lines: 5 dotnet add subcommand definitions + dotnetAddCommand group registration

## Decisions Made

- Field command auto-prepends underscore to avoid the caller needing to know the naming convention
- Property command uses caller-provided casing for name (PascalCase by convention, but not enforced)
- Constructor and method commands take raw content string — syntax validation occurs at commit time via Roslyn parse

## Deviations from Plan

None - plan executed exactly as written.

## Issues Encountered

None.

## User Setup Required

None - no external service configuration required.

## Next Phase Readiness

- All `roslyn-nav dotnet add` subcommands are accessible via CLI
- Phase 4 complete — ready for Phase 5 (dotnet edit or future phase)
- The full add pipeline (stage + commit) is end-to-end verified

---
*Phase: 04-dotnet-add*
*Completed: 2026-02-27*
