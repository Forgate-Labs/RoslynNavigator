---
phase: 03-query-integration
plan: 02
subsystem: Query CLI
tags: [snapshot-query, sql, json-output, cli]
dependency_graph:
  requires:
    - ASK-01: "User can run arbitrary SQL queries against snapshot with JSON output"
    - ASK-02: "Output shape is stable - array of objects"
    - ASK-03: "Query never mutates snapshot files"
    - 03-01: "SqlReadOnlyGuard must be implemented first"
  provides:
    - SnapshotQueryCommand: "Command for executing SQL against snapshots"
    - SnapshotQueryResult: "Stable JSON response contract"
  affects:
    - Program.cs: "Added snapshot command group with generate/query subcommands"
tech_stack:
  added:
    - SnapshotQueryCommand: "Query orchestration with guard validation"
    - SnapshotQueryResult: "JSON contract with rows as List<Dictionary>"
  patterns:
    - CLI command group: "snapshot with subcommands"
key_files:
  created:
    - src/RoslynNavigator/Commands/SnapshotQueryCommand.cs: "Query execution command"
    - src/RoslynNavigator/Models/SnapshotQueryResults.cs: "JSON response model"
    - tests/RoslynNavigator.Tests/SnapshotQueryCommandTests.cs: "17 command tests"
  modified:
    - src/RoslynNavigator/Program.cs: "Added snapshot query subcommand"
decisions:
  - "Made snapshot a command group with generate/query subcommands for backward compat"
  - "Used shared SqlReadOnlyGuard for immutability enforcement"
  - "Resolved default DB path using SnapshotPathService when --db not provided"
---

# Phase 03 Plan 02: Snapshot Query Command Summary

## Overview
Shipped the user-facing SQL query surface for snapshots with a stable JSON contract and enforced immutability.

## What Was Built

### SnapshotQueryCommand
- Accepts SQL text and optional db path
- Validates SQL through `SqlReadOnlyGuard` before execution
- Executes query and returns JSON-serializable result
- Handles errors with structured error responses

### SnapshotQueryResult Model
- Stable JSON contract: `rows` is always `List<Dictionary<string, object?>>`
- Preserves primitive types (string/number/bool/null)
- No schema assumptions - supports arbitrary SELECT projections

### CLI Integration
- Refactored `snapshot` into a command group:
  - `snapshot generate --solution <path> [--db <path>]` - existing behavior
  - `snapshot query --sql "..." [--db <path>] [--solution <path>]` - new query
- `--db` is optional - resolves to default path using `SnapshotPathService`

### Test Coverage
- 17 new tests covering:
  - Successful SELECT queries with various projections
  - Read-only enforcement (INSERT, UPDATE, DELETE, PRAGMA, multi-statement)
  - Error handling (missing DB, invalid SQL, empty SQL)
  - Default path resolution
  - Database immutability guarantees

## Test Results

```
dotnet test RoslynNavigator.sln --filter SnapshotQueryCommandTests
Passed! - Failed: 0, Passed: 17, Skipped: 0, Total: 17
```

All 201 tests in solution pass.

## CLI Usage

```bash
# Query with explicit DB path
roslyn-nav snapshot query --sql "SELECT * FROM classes" --db /path/to/snapshot.db

# Query with default DB resolution from solution
roslyn-nav snapshot query --sql "SELECT COUNT(*) FROM methods" --solution myapp.sln

# Reject mutating SQL
roslyn-nav snapshot query --sql "UPDATE classes SET name='x'" --db snapshot.db
# Returns: {"success":false,"errorMessage":"Mutating keyword 'UPDATE' is not allowed..."}
```

## Deviations from Plan

None - plan executed exactly as written.

## Auth Gates

None encountered.

---

## Self-Check: PASSED

- [x] SnapshotQueryCommand.cs created at src/RoslynNavigator/Commands/SnapshotQueryCommand.cs
- [x] SnapshotQueryResults.cs created at src/RoslynNavigator/Models/SnapshotQueryResults.cs
- [x] Program.cs updated with snapshot query subcommand
- [x] SnapshotQueryCommandTests.cs created with 17 tests
- [x] All tests pass (201 total)
- [x] Commit made: 92c9dca
