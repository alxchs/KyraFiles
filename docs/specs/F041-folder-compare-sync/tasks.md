# F041 — Comparar e sincronizar duas pastas — TASKS

Status: RASCUNHO (aguardando aprovação para iniciar implementação).

Regras: seguir `plan.md`; build Release x64 sem erros; modo espelho (apagar no destino) fica fora desta versão (`spec.md §8`); manter gate F021 e `check-legacy-names.ps1` verdes.

- [ ] **T1 — Motor `FolderCompareService`.** Implementar comparação assíncrona entre duas pastas com modos `SizeAndDate`, `ContentHash` e `NamesOnly`, filtros de exclusão (`*.tmp`, `.git`), tolerância de data (AC-6) e tratamento de colisão arquivo/pasta e symlinks. Atende: US1, US3, AC-4, AC-6, FR-003.
- [ ] **T2 — `FolderCompareViewModel` e integração com `IOperationQueue`.** Gerenciar marcação de itens, direção (`Esq→Dir`, `Dir→Esq`, `Bidirecional`), prévia obrigatória (FR-001), envio à fila de cópias (FR-002) e cancelamento com resumo (AC-5). Atende: US2, AC-1, AC-2, AC-5, FR-001, FR-002.
- [ ] **T3 — UI e ação a partir do painel duplo.** Criar tela/painel de comparação e ação em `Actions/` lendo `ShellPanesPage` (`GetPane(0)` e `GetPane(1)` via `IMultiPanesContext`). Atende: US1, US2.
- [ ] **T4 — Benchmark NFR-001 (100 mil arquivos/lado) e fechamento.** Medir tempo até primeiro resultado (≤ 5 s) e responsividade da UI; validar ACs 1, 2, 4, 5, 6; rodar gate F021 e `check-legacy-names.ps1`; atualizar `STATUS.md` e `AGENT_CONTEXT.md`. Atende: NFR-001.
