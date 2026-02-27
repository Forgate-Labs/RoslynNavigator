---
phase: 02-rules-engine
plan: 03
subsystem: Rules Engine
tags: [rules, check-command, cli, filtering, violations, tdd]
dependency_graph:
  requires:
    - RULE-02 (rule loader from 02-01)
    - RULE-03 (SQL compilation from 02-02)
    - RULE-04 (NOT EXISTS semantics from 02-02)
  provides:
    - RULE-01 (CLI command for rule checking)
    - RULE-05 (output filtering by severity and ruleId)
  affects:
    - Phase 3 (query integration)
tech_stack:
  added:
    - CheckCommand orchestration
    - CheckCommandResult models
    - CLI filtering options
  patterns:
    - Follows existing SnapshotCommand pattern
    - JSON serialization with existing jsonOptions
    - Error handling via OutputError pattern
key_files:
  created:
    - src/RoslynNavigator/Commands/CheckCommand.cs
    - src/RoslynNavigator/Models/CheckCommandResults.cs
    - tests/RoslynNavigator.Tests/CheckCommandTests.cs
  modified:
    - src/RoslynNavigator/Program.cs (added check command)
    - src/RoslynNavigator/Services/RuleSqlCompiler.cs (fixed predicate handling)
decisions:
  - "Reuse SnapshotCommand pattern for consistency"
  - "Filter applied in-memory after evaluation for simplicity"
  - "--db required to ensure snapshot exists before check"
metrics:
  duration: "~10 minutes"
  completed_date: "2026-02-27"
  tasks_completed: 3
  tests_passed: 123 (10 new)
---

# Phase 02 Plan 03: Check Command - CLI Integration Summary

**One-liner:** Check command with --db, --severity, --ruleId filters for RULE-01 and RULE-05.

## Objective

Expose the rules engine through the CLI with production-ready filtering and output contracts. Purpose: RULE-01 and RULE-05 are user-facing and complete the phase value.

## Completed Tasks

| Task | Name | Commit | Files |
|------|------|--------|-------|
| 1 | Implement check command orchestration and result models | d568377 | CheckCommand.cs, CheckCommandResults.cs |
| 2 | Wire check command and filter options in Program.cs | d568377 | Program.cs |
| 3 | Add command-level tests for check output and filtering | d568377 | CheckCommandTests.cs |

## Verification

- `dotnet build RoslynNavigator.sln` - Build succeeds with no errors
- `dotnet test --filter CheckCommandTests` - 10 tests pass
- `dotnet test RoslynNavigator.sln` - All 123 tests pass
- `roslyn-nav check --help` - Shows --db, --severity, --ruleId options

## Artifacts

- **CheckCommand** orchestrates: loads rules via RuleLoaderService, evaluates via RuleEvaluatorService, applies filters
- **CheckCommandResult** provides: DbPath, TotalRulesEvaluated, TotalViolations, FilteredViolations, Violations list
- **CLI options**:
  - `--db` - Required path to snapshot database
  - `--severity` - Filter by error/warning/info
  - `--ruleId` - Filter by rule ID (partial match)

## Success Criteria Met

- [x] `roslyn-nav check` returns JSON violations for snapshot data
- [x] `--severity` and `--ruleId` filters implemented and test-covered
- [x] Command follows existing CLI JSON and error contract patterns

## Deviations from Plan

### Auto-fixed Issues

**1. [Rule 1 - Bug] Fixed SQL compilation for missing predicate fields**
- **Found during:** Task 1 - testing CheckCommand
- **Issue:** RuleSqlCompiler didn't handle `accesses_db`, `calls_external`, `filters_by_tenant` predicates, causing empty WHERE clauses
- **Fix:** Added handlers for these boolean predicates in RuleSqlCompiler.cs
- **Files modified:** src/RoslynNavigator/Services/RuleSqlCompiler.cs
- **Commit:** d568377

**2. [Rule 1 - Bug] Fixed empty NOT EXISTS clause causing SQL syntax error**
- **Found during:** Task 1 - testing CheckCommand
- **Issue:** When nested predicate had no recognized conditions, NOT EXISTS subquery had empty WHERE causing "near ')': syntax error"
- **Fix:** Added check for empty nestedWhere before adding NOT EXISTS clause
- **Files modified:** src/RoslynNavigator/Services/RuleSqlCompiler.cs
- **Commit:** d568377

## Auth Gates

None.
