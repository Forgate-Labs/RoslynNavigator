---
phase: 05-dotnet-update-remove-docs
plan: 01
subsystem: api
tags: [roslyn, csharp, syntax-tree, member-replacement, member-removal]

# Dependency graph
requires:
  - phase: 04-dotnet-add
    provides: DotnetAddMemberService patterns (DetectIndentation, ApplyIndentation, type dispatch switch)
provides:
  - DotnetUpdateRemoveService static class with UpdateMember and RemoveMember
  - UpdateMemberResult and RemoveMemberResult records
  - Underscore-tolerant field name matching helper (FieldNameMatches)
affects: [05-02-CLI-wiring, future-service-extensions]

# Tech tracking
tech-stack:
  added: []
  patterns:
    - "root.ReplaceNode(oldNode, newNode) for in-tree replacement without full reparse"
    - "typeDecl switch expression for ClassDeclarationSyntax/RecordDeclarationSyntax/StructDeclarationSyntax dispatch"
    - "FieldNameMatches: v.Identifier.Text == memberName OR v.Identifier.Text.TrimStart('_') == memberName.TrimStart('_')"

key-files:
  created:
    - src/RoslynNavigator/Services/DotnetUpdateRemoveService.cs
    - tests/RoslynNavigator.Tests/DotnetUpdateRemoveServiceTests.cs
  modified: []

key-decisions:
  - "DetectIndentation/ApplyIndentation helpers duplicated privately from DotnetAddMemberService (not imported) per plan constraint"
  - "UpdateMember validates newContent syntax before locating the member to replace — fail-fast on bad input"
  - "RemoveMember uses WithMembers(Members.Remove(foundMember)) via concrete-type switch expression, same pattern as AddMember"
  - "Underscore-tolerant matching normalizes both sides: strip leading underscore from stored name and from memberName before comparing"

patterns-established:
  - "Validate-then-Locate pattern: syntax-check newContent first, then find target member, then apply replacement"
  - "Private FieldNameMatches helper centralizes underscore-tolerant field lookup for both UpdateMember and RemoveMember"

requirements-completed: [DUPD-01, DUPD-02, DREM-01, DREM-02, DREM-03, CROSS-03]

# Metrics
duration: 4min
completed: 2026-02-27
---

# Phase 5 Plan 01: DotnetUpdateRemoveService Summary

**Roslyn-based member replacement and removal (UpdateMember + RemoveMember) with underscore-tolerant field matching via root.ReplaceNode and WithMembers dispatch**

## Performance

- **Duration:** 4 min
- **Started:** 2026-02-27T11:39:59Z
- **Completed:** 2026-02-27T11:43:00Z
- **Tasks:** 2 (TDD RED + GREEN)
- **Files modified:** 2

## Accomplishments
- `DotnetUpdateRemoveService` static class with `UpdateMember` (replaces property/field by name) and `RemoveMember` (deletes method/property/field by name)
- `UpdateMemberResult` and `RemoveMemberResult` records with `Success`, `ModifiedSource`, and `Error` fields
- 13 xUnit tests covering all plan-specified scenarios: class/struct/record targets, underscore-tolerant field matching, not-found errors, and invalid-syntax rejection
- All tests pass; build succeeds with zero errors

## Task Commits

Each task was committed atomically:

1. **TDD RED — failing tests** - `2727da0` (test)
2. **TDD GREEN — implementation** - `6ef8d52` (feat)

**Plan metadata:** (docs commit below)

_Note: TDD plan — two commits: test (RED) then feat (GREEN)._

## Files Created/Modified
- `src/RoslynNavigator/Services/DotnetUpdateRemoveService.cs` - Static service implementing UpdateMember and RemoveMember with Roslyn APIs
- `tests/RoslynNavigator.Tests/DotnetUpdateRemoveServiceTests.cs` - 13 xUnit facts covering all update/remove scenarios

## Decisions Made
- Duplicated `DetectIndentation` and `ApplyIndentation` privately rather than importing from `DotnetAddMemberService` — satisfies plan constraint, avoids cross-service coupling
- Syntax validation happens before member lookup in `UpdateMember` — invalid content fails before any tree traversal
- `FieldNameMatches` centralizes underscore-tolerant logic for both `UpdateMember` and `RemoveMember` paths to avoid duplication

## Deviations from Plan

None - plan executed exactly as written.

## Issues Encountered

None.

## User Setup Required

None - no external service configuration required.

## Next Phase Readiness

- `DotnetUpdateRemoveService` is fully tested and ready for CLI wiring in plan 05-02
- `UpdateMemberResult` and `RemoveMemberResult` are public records consumable by command handlers
- No blockers

---
*Phase: 05-dotnet-update-remove-docs*
*Completed: 2026-02-27*

## Self-Check: PASSED

- FOUND: src/RoslynNavigator/Services/DotnetUpdateRemoveService.cs
- FOUND: tests/RoslynNavigator.Tests/DotnetUpdateRemoveServiceTests.cs
- FOUND: .planning/phases/05-dotnet-update-remove-docs/05-01-SUMMARY.md
- FOUND commit: 2727da0 (TDD RED)
- FOUND commit: 6ef8d52 (TDD GREEN)
