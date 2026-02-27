---
phase: 02-rules-engine
plan: 02
subsystem: Rules Engine
tags: [rules, sql-compiler, evaluator, violations, tdd, like, not-exists]
dependency_graph:
  requires:
    - RULE-02 (rule loader from 02-01)
    - SNAP-01 (snapshot generation)
  provides:
    - RULE-03 (SQL compilation for wildcard predicates)
    - RULE-04 (NOT EXISTS semantics for negation)
  affects:
    - Plan 02-03 (rule check command)
tech_stack:
  added:
    - SQL compilation patterns
    - Parameterized queries
  patterns:
    - SQL LIKE wildcards (* -> % conversion)
    - NOT EXISTS subqueries for negation
    - Read-only query enforcement
    - In-memory SQLite testing with temp files
key_files:
  created:
    - src/RoslynNavigator/Models/RuleViolationModels.cs
    - src/RoslynNavigator/Services/RuleSqlCompiler.cs
    - src/RoslynNavigator/Services/RuleEvaluatorService.cs
    - tests/RoslynNavigator.Tests/RuleSqlCompilerTests.cs
    - tests/RoslynNavigator.Tests/RuleEvaluatorServiceTests.cs
  modified:
    - tests/RoslynNavigator.Tests (added new test files)
decisions:
  - "Parameterized SQL for safety - prevents SQL injection"
  - "Read-only enforcement - only SELECT queries allowed"
  - "LIKE wildcards for pattern matching (*.Controller -> %.Controller)"
  - "NOT EXISTS for negation - correct boolean logic"
  - "Temp file DB for tests - shared connection for in-memory SQLite"
metrics:
  duration: "~30 minutes"
  completed_date: "2026-02-27"
  tasks_completed: 3
  tests_passed: 113 (19 new)
---

# Phase 02 Plan 02: Rule SQL Compilation and Evaluation Summary

**One-liner:** SQL compiler with LIKE/NOT EXISTS semantics and evaluator service for RULE-03 and RULE-04.

## Objective

Implement SQL compilation and evaluation semantics with TDD so wildcard and negation behavior is locked by tests before CLI wiring. Output: SQL compiler, evaluator service, violation models, and green semantic tests.

## Completed Tasks

| Task | Name | Commit | Files |
|------|------|--------|-------|
| 1 | RED - add failing tests | ea1e86e | RuleSqlCompilerTests.cs, RuleEvaluatorServiceTests.cs |
| 2 | GREEN - implement compiler/evaluator | cd8cc08 | RuleSqlCompiler.cs, RuleEvaluatorService.cs, RuleViolationModels.cs |
| 3 | REFACTOR - clean code | fb8278e | (no changes needed) |

## Verification

- `dotnet test RoslynNavigator.sln --filter "RuleSqlCompilerTests|RuleEvaluatorServiceTests"` - 19 tests pass
- `dotnet test RoslynNavigator.sln` - All 113 tests pass

## Artifacts

- **RuleSqlCompiler** translates rule predicates to parameterized SQL:
  - `calls: IRepo.*` → SQL LIKE with `%` wildcard
  - `not:` → NOT EXISTS subquery
  - `fromNamespace: *.Data.*` → namespace LIKE pattern
  - Boolean flags (returns_null, cognitive_complexity, has_try_catch)

- **RuleEvaluatorService** executes compiled SQL against snapshot:
  - Read-only enforcement (SELECT only)
  - Returns typed RuleViolation rows
  - Handles multiple rule evaluation

- **RuleViolationModels** provides:
  - `RuleViolation` - single violation with ruleId, severity, message, entity context
  - `RuleEvaluationResult` - aggregate result with violations list

## Success Criteria Met

- [x] Wildcard calls predicates enforced through LIKE semantics
- [x] `not:` blocks enforced through NOT EXISTS semantics  
- [x] Evaluator returns typed violations from snapshot data using read-only queries

## Deviations from Plan

None - plan executed exactly as written with TDD approach.

## Auth Gates

None.
