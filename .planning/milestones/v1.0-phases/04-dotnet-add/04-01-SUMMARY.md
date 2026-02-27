---
phase: 04-dotnet-add
plan: 01
subsystem: api
tags: [roslyn, csharp, syntax-tree, member-insertion, tdd]

# Dependency graph
requires:
  - phase: 03-dotnet-scaffold
    provides: DotnetScaffoldCommand pattern — static service delegated to by CLI command
provides:
  - DotnetAddMemberService.AddMember — inserts field/property/constructor/method into class/record/struct with correct ordering
  - DotnetAddMemberService.AddUsing — deduplicates and inserts using directives in sorted order
  - AddMemberResult and AddUsingResult records
affects: [04-dotnet-add plan 02+, any plan wiring dotnet add CLI commands]

# Tech tracking
tech-stack:
  added: []
  patterns:
    - "GetMembers() helper extracts SyntaxList<MemberDeclarationSyntax> from any BaseTypeDeclarationSyntax subtype"
    - "Switch expression on concrete Roslyn syntax node type for WithMembers dispatch"
    - "Syntax validation: wrap content in temp class, parse, check DiagnosticSeverity.Error"
    - "Indentation detection: first member leading trivia last line"

key-files:
  created:
    - src/RoslynNavigator/Services/DotnetAddMemberService.cs
    - tests/RoslynNavigator.Tests/DotnetAddMemberServiceTests.cs
  modified: []

key-decisions:
  - "BaseTypeDeclarationSyntax lacks Members/WithMembers — requires concrete-type switch expression for dispatch (ClassDeclarationSyntax, RecordDeclarationSyntax, StructDeclarationSyntax)"
  - "SyntaxFactory.UsingDirective requires explicit SyntaxFactory.Space leading trivia on the name node to produce 'using X;' not 'usingX;'"
  - "AddUsing inserts in alphabetical order using string.Compare(Ordinal) — consistent sorted block"
  - "Indentation applied per-line before parsing to produce correct leading trivia in inserted member"

patterns-established:
  - "GetMembers() helper: single place to unify member access across class/record/struct"
  - "Syntax validation via temp-class wrapping: validate content before touching real AST"

requirements-completed: [DADD-01, DADD-02, DADD-03, DADD-04, DADD-05, DADD-06, CROSS-03]

# Metrics
duration: 8min
completed: 2026-02-27
---

# Phase 4 Plan 1: DotnetAddMemberService Summary

**Roslyn-based member insertion service: field/property/constructor/method ordering, indentation detection, using dedup, and syntax validation across class/record/struct — 16 tests passing**

## Performance

- **Duration:** 8 min
- **Started:** 2026-02-27T04:16:39Z
- **Completed:** 2026-02-27T04:24:00Z
- **Tasks:** 2 (RED + GREEN, no refactor needed)
- **Files modified:** 2

## Accomplishments

- `DotnetAddMemberService.AddMember` correctly places members in canonical C# order: fields → properties → constructors → methods
- `DotnetAddMemberService.AddUsing` deduplicates using directives and inserts new ones in alphabetical sorted order
- Syntax validation rejects invalid content (returns `AddMemberResult { Success=false }`) without throwing exceptions
- Indentation automatically detected from existing members (supports both spaces and tabs)
- Works across `ClassDeclarationSyntax`, `RecordDeclarationSyntax`, and `StructDeclarationSyntax`
- 16 xUnit tests covering all ordering permutations, error cases, struct, record, and using dedup

## Task Commits

Each task was committed atomically:

1. **Task 1: RED — failing tests** - `133296a` (test)
2. **Task 2: GREEN — implementation** - `1f94318` (feat)

_Note: TDD tasks have separate test and implementation commits_

## Files Created/Modified

- `src/RoslynNavigator/Services/DotnetAddMemberService.cs` — Static service with AddMember and AddUsing methods, AddMemberResult and AddUsingResult records
- `tests/RoslynNavigator.Tests/DotnetAddMemberServiceTests.cs` — 16 xUnit tests for all insertion scenarios, error cases, and edge cases

## Decisions Made

- `BaseTypeDeclarationSyntax` lacks `Members` and `WithMembers` at the base class level — concrete type switch expression required for dispatch
- `SyntaxFactory.UsingDirective(nameNode)` produces `usingX;` without explicit space trivia; fixed by adding `SyntaxFactory.Space` as leading trivia on the name node
- Using directives inserted alphabetically (Ordinal comparison) for consistency
- `AddMember` applies indentation per-line before parsing so the inserted member carries correct leading trivia in the final AST

## Deviations from Plan

### Auto-fixed Issues

**1. [Rule 1 - Bug] BaseTypeDeclarationSyntax members access requires concrete type dispatch**
- **Found during:** GREEN implementation (compilation)
- **Issue:** Plan suggested `typeDecl.Members` on `BaseTypeDeclarationSyntax`, but that property does not exist — only concrete subtypes (ClassDeclarationSyntax etc.) expose Members/WithMembers
- **Fix:** Added `GetMembers()` helper returning `SyntaxList<MemberDeclarationSyntax>` via switch expression on concrete type; `WithMembers` dispatch also uses a switch expression
- **Files modified:** `src/RoslynNavigator/Services/DotnetAddMemberService.cs`
- **Verification:** Build succeeded, all 16 tests pass
- **Committed in:** `1f94318`

**2. [Rule 1 - Bug] SyntaxFactory.UsingDirective missing space trivia produces malformed output**
- **Found during:** GREEN implementation (2 test failures: AddUsing tests)
- **Issue:** `SyntaxFactory.UsingDirective(nameNode)` emits `usingSystem.X;` with no space — `using` keyword is immediately followed by namespace name
- **Fix:** Added `.WithLeadingTrivia(SyntaxFactory.Space)` to the name node before constructing the directive
- **Files modified:** `src/RoslynNavigator/Services/DotnetAddMemberService.cs`
- **Verification:** `AddUsing_NoExistingUsings_UsingAddedAtTop` and `AddUsing_NewUsingWithExistingUsings_UsingAddedAlongsideExisting` both pass
- **Committed in:** `1f94318`

---

**Total deviations:** 2 auto-fixed (2x Rule 1 - Bug)
**Impact on plan:** Both were API surface corrections from the Roslyn implementation detail — no scope creep.

## Issues Encountered

None beyond the auto-fixed deviations above.

## User Setup Required

None - no external service configuration required.

## Next Phase Readiness

- `DotnetAddMemberService` is complete and tested — ready for CLI command wiring in plan 04-02+
- All six `dotnet add` commands (field, property, constructor, method, using, and any composite) can delegate directly to this service
- No blockers

---
*Phase: 04-dotnet-add*
*Completed: 2026-02-27*
