# Roadmap: RoslynNavigator

## Milestones

- ✅ **v1.0 File & Dotnet Commands** — Phases 1-5 (shipped 2026-02-27)
- 🔄 **v2.0 Snapshot, Rules & Ask** — Phases 1-4 (in progress)

## Phases

- [ ] **Phase 1: Snapshot Foundation** - Gera banco SQLite com classes, métodos, dependências, chamadas, annotations, flags e metadados
- [ ] **Phase 2: Rules Engine** - Avalia regras YAML builtin/domain e reporta violações filtráveis
- [ ] **Phase 3: Query Integration** - Consulta SQL arbitrária com output JSON para LLM
- [ ] **Phase 4: Integration & Polish** - Novos projetos integrados, CLI atualizado, compatibilidade garantida

## Phase Details

### Phase 1: Snapshot Foundation

**Goal:** Usuário pode gerar snapshot SQLite da solution com estrutura completa de tabelas e sinais de análise

**Depends on:** Nothing (first phase of milestone)

**Requirements:** SNAP-01, SNAP-02, SNAP-03, SNAP-04

**Success Criteria** (what must be TRUE):
1. Usuário executa `roslyn-nav snapshot --solution <path.sln>` e recebe arquivo `.db` criado no caminho padrão
2. Banco SQLite contém tabelas `classes`, `methods`, `dependencies`, `calls`, `annotations`, `flags`, `snapshot_meta` conforme schema
3. Cada classe/método persistido inclui sinais de análise: `returns_null`, `cognitive_complexity`, `has_try_catch`, `calls_external`, `accesses_db`, `filters_by_tenant`
4. Schema é carregado de `Schema.sql` como embedded resource e aplicado com migration idempotente TBD

---



**Plans:** 3 plans

Plans:
- [ ] 01-01-PLAN.md — Build SQLite schema foundation (embedded resource + bootstrap/path services)
- [ ] 01-02-PLAN.md — TDD snapshot extraction and analysis signal persistence
- [ ] 01-03-PLAN.md — Expose `snapshot` CLI command and verify end-to-end behavior

---

### Phase 2: Rules Engine

**Goal:** Usuário pode avaliar regras sobre snapshot existente e reportar violações filtráveis

**Depends on:** Phase 1 (Snapshot Foundation)

**Requirements:** RULE-01, RULE-02, RULE-03, RULE-04, RULE-05

**Success Criteria** (what must be TRUE):
1. Usuário executa `roslyn-nav check` e recebe JSON com violações encontradas no snapshot
2. Engine carrega regras builtin (`architecture.yaml`, `code-quality.yaml`, `security.yaml`) e regras opcionais em `roslyn-nav-rules/`
3. Predicados com wildcard (`IRepo.*`) são avaliados corretamente via LIKE
4. Bloco `not:` em regras é avaliado com semântica NOT EXISTS
5. Output JSON inclui filtros por severidade e ruleId, permitindo `roslyn-nav check --severity error --ruleId no-naked-strings`

**Plans:** TBD

---

### Phase 3: Query Integration

**Goal:** Usuário pode executar SQL arbitrário no snapshot e receber JSON para consumo por LLM externa

**Depends on:** Phase 1 (Snapshot Foundation)

**Requirements:** ASK-01, ASK-02, ASK-03

**Success Criteria** (what must be TRUE):
1. Usuário executa `roslyn-nav snapshot query --sql "SELECT * FROM classes LIMIT 10" [--db <path>]` e recebe JSON array
2. Output JSON segue estrutura consistente independente da query (sempre array de objetos)
3. Operações `check` e `snapshot query` são somente leitura no arquivo `.db` (snapshot permanece imutável)

**Plans:** TBD

---

### Phase 4: Integration & Polish

**Goal:** Novos projetos integrados à solution CLI, comandos aparecem no help, compatibilidade mantida

**Depends on:** Phase 2, Phase 3

**Requirements:** INT-01, INT-02, INT-03

**Success Criteria** (what must be TRUE):
1. Solution contém `RoslynNavigator.Snapshot` e `RoslynNavigator.Rules` com ProjectReference em `RoslynNavigator`
2. Comandos `snapshot`, `check`, `snapshot query` aparecem em `roslyn-nav --help` e seguem padrão JSON dos comandos existentes
3. Todos os 41 comandos anteriores (navegação + write/mutation) continuam funcionando exatamente como antes

**Plans:** TBD

---

## Progress Table

| Phase | Plans Complete | Status | Completed |
|-------|----------------|--------|-----------|
| 1. Snapshot Foundation | 0/3 | Not started | - |
| 2. Rules Engine | 0/1 | Not started | - |
| 3. Query Integration | 0/1 | Not started | - |
| 4. Integration & Polish | 0/1 | Not started | - |

---

## Coverage

- **Total v2.0 requirements:** 15
- **Mapped to phases:** 15
- **Unmapped:** 0

| Requirement | Phase |
|-------------|-------|
| SNAP-01 | Phase 1 |
| SNAP-02 | Phase 1 |
| SNAP-03 | Phase 1 |
| SNAP-04 | Phase 1 |
| RULE-01 | Phase 2 |
| RULE-02 | Phase 2 |
| RULE-03 | Phase 2 |
| RULE-04 | Phase 2 |
| RULE-05 | Phase 2 |
| ASK-01 | Phase 3 |
| ASK-02 | Phase 3 |
| ASK-03 | Phase 3 |
| INT-01 | Phase 4 |
| INT-02 | Phase 4 |
| INT-03 | Phase 4 |

---

*Roadmap created: 2026-02-27 for milestone v2.0 Snapshot, Rules & Ask*
