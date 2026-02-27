# Requirements: RoslynNavigator

**Defined:** 2026-02-27
**Core Value:** O assistente de IA consegue navegar, criar e modificar código C# com precisão cirúrgica sem precisar ler arquivos inteiros — reduzindo tokens e eliminando edições ambíguas.

## Milestone v2.0 Requirements

### Snapshot

- [x] **SNAP-01**: Usuário pode gerar snapshot SQLite da solution com `roslyn-nav snapshot --solution <path.sln>`
- [x] **SNAP-02**: Snapshot inclui tabelas `classes`, `methods`, `dependencies`, `calls`, `annotations`, `flags`, `snapshot_meta` conforme schema definido
- [x] **SNAP-03**: Snapshot persiste sinais de análise (`returns_null`, `cognitive_complexity`, `has_try_catch`, `calls_external`, `accesses_db`, `filters_by_tenant`)
- [x] **SNAP-04**: Snapshot usa `Schema.sql` como embedded resource e gera DB com caminho padrão configurado

### Rules / Check

- [x] **RULE-01**: Usuário pode rodar `roslyn-nav check` para avaliar regras sobre snapshot existente
- [x] **RULE-02**: Engine carrega regras builtin embedded (`architecture.yaml`, `code-quality.yaml`, `security.yaml`) e regras domain opcionais em `roslyn-nav-rules/`
- [x] **RULE-03**: Predicados `calls` com wildcard (`IRepo.*`) são avaliados via `LIKE`
- [x] **RULE-04**: Bloco `not:` é avaliado com semântica `NOT EXISTS`
- [x] **RULE-05**: `check` suporta filtros por severidade e por `ruleId` no output JSON

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
| SNAP-01 | Phase 1 | Complete |
| SNAP-02 | Phase 1 | Complete |
| SNAP-03 | Phase 1 | Complete |
| SNAP-04 | Phase 1 | Complete |
| RULE-01 | Phase 2 | Complete |
| RULE-02 | Phase 2 | Complete |
| RULE-03 | Phase 2 | Complete |
| RULE-04 | Phase 2 | Complete |
| RULE-05 | Phase 2 | Complete |
| ASK-01 | Phase 3 | Pending |
| ASK-02 | Phase 3 | Pending |
| ASK-03 | Phase 3 | Pending |
| INT-01 | Phase 4 | Pending |
| INT-02 | Phase 4 | Pending |
| INT-03 | Phase 4 | Pending |

**Coverage:**
- Milestone requirements: 15 total
- Mapped to phases: 15 ✓
- Unmapped: 0 ✓

---
*Requirements defined: 2026-02-27*
*Last updated: 2026-02-27 after Phase 2 (Rules Engine) verification - all 5 RULE requirements complete*
