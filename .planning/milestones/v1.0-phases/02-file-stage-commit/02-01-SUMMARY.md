---
phase: 02-file-stage-commit
plan: 01
subsystem: FilePlanEngine
tags: [tdd, engine, validation, diff, atomicity, rollback]
dependency_graph:
  requires: [BackupService, PlanModels]
  provides: [FilePlanEngine, ApplyResult]
  affects: [file-commit commands, file-status commands, file-rollback commands]
tech_stack:
  added: [xunit test project, LCS diff algorithm]
  patterns: [TDD red-green, atomic in-memory apply before disk write]
key_files:
  created:
    - src/RoslynNavigator/Services/FilePlanEngine.cs
    - tests/RoslynNavigator.Tests/FilePlanEngineTests.cs
    - tests/RoslynNavigator.Tests/RoslynNavigator.Tests.csproj
  modified:
    - RoslynNavigator.sln
decisions:
  - ComputeDiff captures original lines before writing to disk — avoids empty diff when called after CommitAsync writes
  - LCS-based Myers-style diff implemented inline (no external library) per plan constraint
  - ComputeDiffFromMemory private helper used by both CommitAsync and public ComputeDiff for DRY
  - Test project uses Path.GetTempPath() + Guid directories with IDisposable cleanup for isolation
metrics:
  duration: 3 min
  completed: 2026-02-27
  tasks: 3
  files: 4
---

# Phase 02 Plan 01: FilePlanEngine — Validation, Diff, Atomic Apply, Rollback Summary

**One-liner:** LCS-based FilePlanEngine with ValidateAsync/CommitAsync/RollbackAsync and atomic file writes using in-memory pre-computation before any disk write.

## What Was Built

`FilePlanEngine` is the stateless business-logic service that all commit-flow commands delegate to. It exposes four public methods:

- **`ValidateAsync`** — validates Edit/Delete ops by checking that the expected `OldContent` matches the actual line content on disk. Returns specific error messages for wrong content, file not found, and line out of range. Write/Append ops pass validation unconditionally.
- **`ComputeDiff`** — computes a unified diff (LCS-based, 3-line context) from in-memory original and modified file states without touching disk.
- **`CommitAsync`** — validates all ops first; if any fail, throws `InvalidOperationException` without modifying any file (atomicity guarantee). On success: backup → apply in-memory → write all files → return `ApplyResult { UnifiedDiff, BackupPath }`.
- **`RollbackAsync`** — copies all files from a backup directory back to the working directory, preserving relative paths. Throws if backup path does not exist.

The xUnit test project was created from scratch (`tests/RoslynNavigator.Tests/`) and added to the solution with a project reference to the main project.

## Test Coverage

13 test methods covering all cases from the plan spec:

| Test | Behavior verified |
|------|-------------------|
| `ValidateAsync_EditOpLineMatches_ReturnsNoErrors` | Happy path Edit |
| `ValidateAsync_EditOpLineDoesNotMatch_ReturnsSpecificError` | Edit mismatch error |
| `ValidateAsync_EditOpFileNotFound_ReturnsFileNotFoundError` | Missing file error |
| `ValidateAsync_EditOpLineOutOfRange_ReturnsOutOfRangeError` | Out of range error |
| `ValidateAsync_DeleteOpLineMatches_ReturnsNoErrors` | Happy path Delete |
| `ValidateAsync_DeleteOpLineDoesNotMatch_ReturnsSpecificError` | Delete mismatch error |
| `ValidateAsync_WriteOpFileDoesNotExist_ReturnsNoErrors` | Write no-validation |
| `ValidateAsync_AppendOpFileDoesNotExist_ReturnsNoErrors` | Append no-validation |
| `CommitAsync_AllValidOps_WritesFilesAndReturnsDiffAndBackupPath` | Full commit happy path |
| `CommitAsync_InvalidOp_ThrowsAndDoesNotModifyAnyFile` | Atomic failure guarantee |
| `RollbackAsync_ValidBackupPath_RestoresFilesToOriginalContent` | Rollback happy path |
| `RollbackAsync_MissingBackupPath_ThrowsInvalidOperationException` | Rollback missing backup |
| `ComputeDiff_EditOp_ReturnsUnifiedDiffWithCorrectFormat` | Diff format verification |

All 13 tests pass.

## Deviations from Plan

### Auto-fixed Issues

**1. [Rule 1 - Bug] Fixed empty diff in CommitAsync**
- **Found during:** GREEN phase (test run)
- **Issue:** `CommitAsync` called `ComputeDiff` after writing files to disk, so `LoadOriginalLines` read the already-modified files — producing an empty diff (original == modified).
- **Fix:** Captured original lines and computed new state in memory before writing; extracted `ComputeDiffFromMemory` private helper that accepts pre-loaded data. `CommitAsync` now computes the diff between in-memory states before any disk write.
- **Files modified:** `src/RoslynNavigator/Services/FilePlanEngine.cs`
- **Commit:** 55ec73b (included in green commit)

## Requirements Satisfied

- FSTAGE-01: ValidateAsync rejects Edit ops with wrong old string with specific error messages
- FSTAGE-04: ValidateAsync rejects Delete ops with wrong old string with specific error messages
- FCOMMIT-02: CommitAsync validates all ops before touching any file (atomicity)
- FCOMMIT-03: RollbackAsync restores files from backup directory
- CROSS-02: Error messages include specific context (wrong line content, file not found, backup not found)

## Self-Check

- [x] `src/RoslynNavigator/Services/FilePlanEngine.cs` — FOUND
- [x] `tests/RoslynNavigator.Tests/FilePlanEngineTests.cs` — FOUND
- [x] Commit fd9817b (RED tests) — FOUND
- [x] Commit 55ec73b (GREEN implementation) — FOUND
- [x] All 13 tests pass — VERIFIED
- [x] Build succeeds 0 errors — VERIFIED

## Self-Check: PASSED
