# Roadmap: RoslynNavigator

## Milestones

- ✅ **v1.0 File & Dotnet Commands** — Phases 1-5 (shipped 2026-02-27)
- 🔄 **v2.0 Snapshot, Rules & Ask** — Phases 6-9 (in progress)

## Phases

- [ ] **Phase 6: Snapshot Foundation** - Gera banco SQLite com classes, métodos, dependências, chamadas, annotations, flags e metadados
- [ ] **Phase 7: Rules Engine** - Avalia regras YAML builtin/domain e reporta violações filtráveis
- [ ] **Phase 8: Query Integration** - Consulta SQL arbitrária com output JSON para LLM
- [ ] **Phase 9: Integration & Polish** - Novos projetos integrados, CLI atualizado, compatibilidade garantida

## Phase Details

### Phase 6: Snapshot Foundation

**Goal:** Usuário pode gerar snapshot SQLite da solution com estrutura completa de tabelas e sinais de análise

**Depends on:** Nothing (first phase of milestone)

**Requirements:** SNAP-01, SNAP-02, SNAP-03, SNAP-04

**Success Criteria** (what must be TRUE):
1. Usuário executa `roslyn-nav snapshot --solution <path.sln>` e recebe arquivo `.db` criado no caminho padrão
2. Banco SQLite contém tabelas `classes`, `methods`, `dependencies`, `calls`, `annotations`, `flags`, `snapshot_meta` conforme schema
3. Cada classe/método persistido inclui sinais de análise: `returns_null`, `cognitive_complexity`, `has_try_catch`, `calls_external`, `accesses_db`, `filters_by_tenant`
4. Schema é carregado de `Schema.sql` como embedded resource e aplicado com migration idempotente TBD

---



**Plans:**### Phase 7: Rules Engine

**Goal:** Usuário pode avaliar regras sobre snapshot existente e reportar violações filtráveis

**Depends on:** Phase 6 (Snapshot Foundation)

**Requirements:** RULE-01, RULE-02, RULE-03, RULE-04, RULE-05

**Success Criteria** (what must be TRUE):
1. Usuário executa `roslyn-nav check` e recebe JSON com violações encontradas no snapshot
2. Engine carrega regras builtin (`architecture.yaml`, `code-quality.yaml`, `security.yaml`) e regras opcionais em `roslyn-nav-rules/`
3. Predicados com wildcard (`IRepo.*`) são avaliados corretamente via LIKE
4. Bloco `not:` em regras é avaliado com semântica NOT EXISTS
5. Output JSON inclui filtros por severidade e ruleId, permitindo `roslyn-nav check --severity error --ruleId no-naked-strings`

**Plans:** TBD

---

### Phase 8: Query Integration

**Goal:** Usuário pode executar SQL arbitrário no snapshot e receber JSON para consumo por LLM externa

**Depends on:** Phase 6 (Snapshot Foundation)

**Requirements:** ASK-01, ASK-02, ASK-03

**Success Criteria** (what must be TRUE):
1. Usuário executa `roslyn-nav snapshot query --sql "SELECT * FROM classes LIMIT 10" [--db <path>]` e recebe JSON array
2. Output JSON segue estrutura consistente independente da query (sempre array de objetos)
3. Operações `check` e `snapshot query` são somente leitura no arquivo `.db` (snapshot permanece imutável)

**Plans:** TBD

---

### Phase 9: Integration & Polish

**Goal:** Novos projetos integrados à solution CLI, comandos aparecem no help, compatibilidade mantida

**Depends on:** Phase 7, Phase 8

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
| 6. Snapshot Foundation | 0/1 | Not started | - |
| 7. Rules Engine | 0/1 | Not started | - |
| 8. Query Integration | 0/1 | Not started | - |
| 9. Integration & Polish | 0/1 | Not started | - |

---

## Coverage

- **Total v2.0 requirements:** 15
- **Mapped to phases:** 15
- **Unmapped:** 0

| Requirement | Phase |
|-------------|-------|
| SNAP-01 | Phase 6 |
| SNAP-02 | Phase 6 |
| SNAP-03 | Phase 6 |
| SNAP-04 | Phase 6 |
| RULE-01 | Phase 7 |
| RULE-02 | Phase 7 |
| RULE-03 | Phase 7 |
| RULE-04 | Phase 7 |
| RULE-05 | Phase 7 |
| ASK-01 | Phase 8 |
| ASK-02 | Phase 8 |
| ASK-03 | Phase 8 |
| INT-01 | Phase 9 |
| INT-02 | Phase 9 |
| INT-03 | Phase 9 |

---

*Roadmap created: 2026-02-27 for milestone v2.0 Snapshot, Rules & Ask*
