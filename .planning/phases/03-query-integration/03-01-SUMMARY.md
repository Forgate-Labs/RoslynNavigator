---
phase: 03-query-integration
plan: 01
subsystem: Query Safety
tags: [sql-guard, security, snapshot, read-only]
dependency_graph:
  requires:
    - ASK-03: "Query execution must reject non-read-only SQL"
  provides:
    - SqlReadOnlyGuard: "Shared service for SQL read-only validation"
  affects:
    - RuleEvaluatorService: "Now uses shared guard"
    - Future query commands: "Can reuse SqlReadOnlyGuard"
tech_stack:
  added:
    - SqlReadOnlyGuard: "SQL validation service with regex-based keyword detection"
  patterns:
    - Guard pattern: "Validate-before-execute for all SQL operations"
key_files:
  created:
    - src/RoslynNavigator/Services/SqlReadOnlyGuard.cs: "Read-only SQL validation service"
    - tests/RoslynNavigator.Tests/SqlReadOnlyGuardTests.cs: "61 tests for guard behavior"
  modified:
    - src/RoslynNavigator/Services/RuleEvaluatorService.cs: "Wired to shared guard"
    - tests/RoslynNavigator.Tests/RuleEvaluatorServiceTests.cs: "Added guard integration tests"
decisions:
  - "Used regex-based keyword detection for mutating SQL detection"
  - "Strip comments before validating to allow commented SQL"
  - "Reject all unknown query types for safety"
---

# Phase 03 Plan 01: SQL Read-Only Guard Summary

## Overview
Hardened SQL execution boundaries so snapshot databases remain immutable during all rule checks. Implemented a reusable `SqlReadOnlyGuard` service and integrated it with `RuleEvaluatorService`.

## What Was Built

### SqlReadOnlyGuard Service
A dedicated service that validates whether SQL is read-only and safe to run against snapshot databases:

- **Valid queries:** Only `SELECT` and `WITH` (CTEs) are allowed
- **Rejected patterns:**
  - Empty or whitespace-only SQL
  - Multi-statement queries (`;` with additional content)
  - Mutating keywords: `INSERT`, `UPDATE`, `DELETE`, `REPLACE`
  - DDL keywords: `CREATE`, `DROP`, `ALTER`, `TRUNCATE`
  - Dangerous operations: `PRAGMA`, `ATTACH`, `DETACH`, `VACUUM`
  - Transaction control: `BEGIN`, `COMMIT`, `ROLLBACK`
  - `EXPLAIN` on non-select queries

- **Comment handling:** Strips comments before validation to allow valid commented SQL

### Integration with RuleEvaluatorService
- Replaced local `IsReadOnlyQuery()` method with `SqlReadOnlyGuard.Validate()`
- Guard rejection messages propagate clearly through `RuleEvaluationResult.ErrorMessage`
- Preserved existing evaluation contract and JSON error behavior

### Test Coverage
- **61 new tests** in `SqlReadOnlyGuardTests` covering:
  - Valid SELECT and WITH queries
  - Queries with comments (single-line, multi-line)
  - All mutating keyword rejections
  - Multi-statement rejection
  - Edge cases (empty, whitespace, comment-only)
- **3 new regression tests** in `RuleEvaluatorServiceTests` proving evaluator uses guard

## Test Results

```
dotnet test RoslynNavigator.sln --filter "SqlReadOnlyGuardTests|RuleEvaluatorServiceTests"
Passed! - Failed: 0, Passed: 68, Skipped: 0, Total: 68
```

All 184 tests in solution pass, including:
- CheckCommandTests: 10 passed
- All other existing tests maintained

## Deviations from Plan

None - plan executed exactly as written.

## Auth Gates

None encountered.

---

## Self-Check: PASSED

- [x] SqlReadOnlyGuard.cs created at src/RoslynNavigator/Services/SqlReadOnlyGuard.cs
- [x] RuleEvaluatorService.cs updated to use SqlReadOnlyGuard
- [x] SqlReadOnlyGuardTests.cs created with 58 tests
- [x] RuleEvaluatorServiceTests.cs updated with 3 additional tests
- [x] All tests pass (184 total)
- [x] Commit made: 61b61ee
