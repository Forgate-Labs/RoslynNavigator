# Roadmap: RoslynNavigator — File & Dotnet Commands

## Overview

The project already has 17 navigation commands. This milestone adds write capability: a plan/commit system for safe file edits and AST-aware dotnet mutations. Phase 1 builds the infrastructure all other phases depend on. Phases 2-4 deliver the `file` and `dotnet` command groups in dependency order. Phase 5 completes the mutation surface and updates CLAUDE.md so AI assistants can use the new commands.

## Phases

- [x] **Phase 1: Infrastructure & File Read** - Plan/commit foundation + immediate read commands (completed 2026-02-27)
- [x] **Phase 2: File Stage & Commit** - Staged file edits with atomic apply, rollback, and diff preview (completed 2026-02-27)
- [x] **Phase 3: Dotnet Scaffold** - Generate new C# files (class, interface, record, enum) (completed 2026-02-27)
- [x] **Phase 4: Dotnet Add** - Insert members (field, property, constructor, method, using) into existing types (completed 2026-02-27)
- [ ] **Phase 5: Dotnet Update, Remove & Docs** - Mutate and remove existing members; update CLAUDE.md

## Phase Details

### Phase 1: Infrastructure & File Read
**Goal**: The plan/commit engine exists and `file read` / `file grep` work so all downstream phases can build on a stable foundation
**Depends on**: Nothing (first phase)
**Requirements**: INFRA-01, INFRA-02, INFRA-03, INFRA-04, FREAD-01, FREAD-02, CROSS-01
**Success Criteria** (what must be TRUE):
  1. `file read <path>` returns file content with line numbers; `--lines START-END` filters to a range
  2. `file grep <pattern> [path] [--ext .cs]` returns matching lines with file/line context
  3. `.roslyn-nav-plans.json` is created on first staged operation and persists between CLI invocations
  4. `IPlanStore` / `FilePlanStore` can be injected into any command without knowing the storage detail
  5. A `file commit` on a clean state creates a timestamped backup in `.roslyn-nav-backup/` before touching any file
**Plans**: 2 plans

Plans:
- [ ] 01-01-PLAN.md — Plan/commit infrastructure: PlanModels, IPlanStore, FilePlanStore, BackupService
- [ ] 01-02-PLAN.md — file read and file grep commands + file subcommand group in Program.cs

### Phase 2: File Stage & Commit
**Goal**: AI can stage a set of file edits and apply them atomically, preview the diff before committing, and roll back if needed
**Depends on**: Phase 1
**Requirements**: FSTAGE-01, FSTAGE-02, FSTAGE-03, FSTAGE-04, FCOMMIT-01, FCOMMIT-02, FCOMMIT-03, FCOMMIT-04, CROSS-02
**Success Criteria** (what must be TRUE):
  1. `file plan edit` rejects the operation when the specified line does not contain the expected old string
  2. `file status` prints a unified diff of all staged changes before anything is written
  3. `file commit` applies all staged changes and returns a unified diff; if any validation fails, zero files are modified
  4. `file rollback` restores all files touched by the last commit from the backup directory
  5. `file clear` removes `.roslyn-nav-plans.json` and discards all staged operations without touching any file
**Plans**: 3 plans

Plans:
- [ ] 02-01-PLAN.md — TDD: FilePlanEngine (validate, diff, atomic apply, rollback)
- [ ] 02-02-PLAN.md — file plan edit/write/append/delete commands + file status
- [ ] 02-03-PLAN.md — file commit, file rollback, file clear commands

### Phase 3: Dotnet Scaffold
**Goal**: AI can create new, correctly structured C# files for class, interface, record, and enum types without reading any existing file
**Depends on**: Phase 1
**Requirements**: SCAF-01, SCAF-02, SCAF-03, SCAF-04
**Success Criteria** (what must be TRUE):
  1. `dotnet scaffold class` produces a valid C# file with file-scoped namespace and minimal `public class` body
  2. `dotnet scaffold interface`, `record`, and `enum` each produce syntactically valid files following the same pattern
  3. Scaffolded files are staged (not written immediately) and become part of the next `file commit`
**Plans**: 1 plan

Plans:
- [ ] 03-01-PLAN.md — DotnetScaffoldCommand (all 4 types) + FilePlanEngine ScaffoldFile support + dotnet scaffold CLI wiring

### Phase 4: Dotnet Add
**Goal**: AI can insert members (using directive, field, property, constructor, method) into existing C# types while respecting conventional member order
**Depends on**: Phase 2, Phase 3
**Requirements**: DADD-01, DADD-02, DADD-03, DADD-04, DADD-05, DADD-06, CROSS-03
**Success Criteria** (what must be TRUE):
  1. `dotnet add method` inserts the method before the closing `}` of the target class and detects existing indentation
  2. `dotnet add field`, `property`, `constructor` each insert at the correct position following fields → properties → constructors → methods order
  3. `dotnet add using` is a no-op (no error, no duplicate) when the directive already exists
  4. All `dotnet add` operations work on `ClassDeclarationSyntax`, `RecordDeclarationSyntax`, and `StructDeclarationSyntax`
  5. Providing syntactically invalid content to any `dotnet add` command returns an error before staging anything
**Plans**: 3 plans

Plans:
- [ ] 04-01-PLAN.md — TDD: DotnetAddMemberService (Roslyn insertion logic for all member kinds + using dedup)
- [ ] 04-02-PLAN.md — DotnetAddResult model + DotnetAddCommand + FilePlanEngine AddMember support
- [ ] 04-03-PLAN.md — Wire dotnet add subcommand group (using/field/property/constructor/method) in Program.cs

### Phase 5: Dotnet Update, Remove & Docs
**Goal**: AI can replace or delete existing members and CLAUDE.md documents all new commands so AI assistants can discover and use them
**Depends on**: Phase 4
**Requirements**: DUPD-01, DUPD-02, DREM-01, DREM-02, DREM-03, CROSS-04
**Success Criteria** (what must be TRUE):
  1. `dotnet update property` and `dotnet update field` replace the target member with provided content; a member name that does not exist returns an error
  2. `dotnet remove method`, `remove property`, and `remove field` remove the named member; removing a non-existent member returns an error
  3. CLAUDE.md contains entries for every new command (`file read`, `file grep`, `file plan edit/write/append/delete`, `file status/commit/rollback/clear`, all `dotnet scaffold` and `dotnet add/update/remove` variants) with usage examples
**Plans**: TBD

## Progress

| Phase | Plans Complete | Status | Completed |
|-------|----------------|--------|-----------|
| 1. Infrastructure & File Read | 2/2 | Complete   | 2026-02-27 |
| 2. File Stage & Commit | 3/3 | Complete   | 2026-02-27 |
| 3. Dotnet Scaffold | 1/1 | Complete   | 2026-02-27 |
| 4. Dotnet Add | 3/3 | Complete   | 2026-02-27 |
| 5. Dotnet Update, Remove & Docs | 0/? | Not started | - |
