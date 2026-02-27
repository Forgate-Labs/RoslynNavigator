# Requirements: RoslynNavigator

**Defined:** 2026-02-27
**Core Value:** O assistente de IA consegue navegar, criar e modificar código C# com precisão cirúrgica sem precisar ler arquivos inteiros — reduzindo tokens e eliminando edições ambíguas.

## Milestone v2.0 Requirements

### Snapshot

- [ ] **SNAP-01**: Usuário pode gerar snapshot SQLite da solution com `roslyn-nav snapshot --solution <path.sln>`
- [ ] **SNAP-02**: Snapshot inclui tabelas `classes`, `methods`, `dependencies`, `calls`, `annotations`, `flags`, `snapshot_meta` conforme schema definido
- [ ] **SNAP-03**: Snapshot persiste sinais de análise (`returns_null`, `cognitive_complexity`, `has_try_catch`, `calls_external`, `accesses_db`, `filters_by_tenant`)
- [ ] **SNAP-04**: Snapshot usa `Schema.sql` como embedded resource e gera DB com caminho padrão configurado

### Rules / Check

- [ ] **RULE-01**: Usuário pode rodar `roslyn-nav check` para avaliar regras sobre snapshot existente
- [ ] **RULE-02**: Engine carrega regras builtin embedded (`architecture.yaml`, `code-quality.yaml`, `security.yaml`) e regras domain opcionais em `roslyn-nav-rules/`
- [ ] **RULE-03**: Predicados `calls` com wildcard (`IRepo.*`) são avaliados via `LIKE`
- [ ] **RULE-04**: Bloco `not:` é avaliado com semântica `NOT EXISTS`
- [ ] **RULE-05**: `check` suporta filtros por severidade e por `ruleId` no output JSON

### Query / Ask Integration

- [ ] **ASK-01**: Usuário pode executar SQL arbitrário com `roslyn-nav snapshot query --sql "<sql>" [--db <path>]`
- [ ] **ASK-02**: `snapshot query` retorna JSON consistente para consumo por LLM externa
- [ ] **ASK-03**: `check` e `snapshot query` são somente leitura no `.db` (snapshot imutável após geração)

### Solution / CLI Integration

- [ ] **INT-01**: Solution inclui `RoslynNavigator.Snapshot` e `RoslynNavigator.Rules` com `ProjectReference` no projeto CLI
- [ ] **INT-02**: Comandos `snapshot`, `check` e `snapshot query` aparecem no `--help` e seguem padrão dos comandos existentes
- [ ] **INT-03**: Expansão não quebra comandos atuais (navegação + write/mutation) e mantém contrato de output JSON

## Future Requirements

Nenhum item adicional mapeado para este milestone.

## Out of Scope

| Feature | Reason |
|---------|--------|
| Projeto separado `RoslynNavigator.Ask` | A LLM permanece externa; `roslyn-nav` é ferramenta de execução e consulta |
| LLM embutida no binário `roslyn-nav` | Escopo é snapshot/rules/query, não inferência dentro do CLI |
| Alterar comandos de navegação já entregues | Milestone foca expansão sem quebrar contratos existentes |

## Traceability

| Requirement | Phase | Status |
|-------------|-------|--------|
| SNAP-01 | Phase 6 - Snapshot Foundation | Pending |
| SNAP-02 | Phase 6 - Snapshot Foundation | Pending |
| SNAP-03 | Phase 6 - Snapshot Foundation | Pending |
| SNAP-04 | Phase 6 - Snapshot Foundation | Pending |
| RULE-01 | Phase 7 - Rules Engine | Pending |
| RULE-02 | Phase 7 - Rules Engine | Pending |
| RULE-03 | Phase 7 - Rules Engine | Pending |
| RULE-04 | Phase 7 - Rules Engine | Pending |
| RULE-05 | Phase 7 - Rules Engine | Pending |
| ASK-01 | Phase 8 - Query Integration | Pending |
| ASK-02 | Phase 8 - Query Integration | Pending |
| ASK-03 | Phase 8 - Query Integration | Pending |
| INT-01 | Phase 9 - Integration & Polish | Pending |
| INT-02 | Phase 9 - Integration & Polish | Pending |
| INT-03 | Phase 9 - Integration & Polish | Pending |

**Coverage:**
- Milestone requirements: 15 total
- Mapped to phases: 15 ✓
- Unmapped: 0 ✓

---
*Requirements defined: 2026-02-27*
*Last updated: 2026-02-27 after milestone v2.0 requirement definition*
