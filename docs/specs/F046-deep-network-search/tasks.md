# F046 — Busca profunda na rede — TASKS

Status: RASCUNHO (aguardando aprovação para iniciar implementação).

Regras: seguir `plan.md`; build Release x64 sem erros; busca por conteúdo na rede fica fora desta versão (`spec.md §8`); manter gate F021 e `check-legacy-names.ps1` verdes.

- [ ] **T0 — Medição `R-rede-baseline`.** Medir tempo, CPU e memória da busca atual em compartilhamento de rede/UNC (vs busca do Windows) e registrar em `docs/agents/evidence/F046/baseline.md`. Atende: NFR-001, histórico §9.
- [ ] **T1 — `NetworkSearchProvider` com streaming, timeout por servidor e cancelamento.** Implementar `ISearchProvider` para caminhos UNC e unidades mapeadas com `OnBatchReceived`, limite de concorrência, timeout por servidor offline e suporte a caminhos longos `\\?\UNC\`. Atende: US1, US2, AC-1, AC-2, AC-3, FR-001, FR-002.
- [ ] **T2 — Descoberta de compartilhamentos, permissões e filtros.** Suportar escopo `\\servidor` (shares acessíveis), contabilizar pastas sem permissão como "inacessíveis" e aplicar filtros de extensão, tamanho, data e profundidade. Atende: US3, AC-4, AC-5, AC-7, FR-003.
- [ ] **T3 — Índice opcional por compartilhamento (desligado por padrão).** Implementar cache em disco opcional por share com TTL configurável, botão "Atualizar agora" e reconciliação rápida. Atende: US4, AC-6, FR-004.
- [ ] **T4 — Integração em `SearchProviderFactory`, medição NFR-001 e fechamento.** Registrar provedor para caminhos de rede, validar ACs 1–7, comparar CPU/RAM/tempo com `R-rede-baseline`, rodar gate F021 e `check-legacy-names.ps1`, atualizar `STATUS.md` e `AGENT_CONTEXT.md`. Atende: NFR-001.
