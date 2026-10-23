# F026 — Tarefas

Status: **APROVADAS** (Claude, 20/10/2026; atualizado em R434 em 22/10/2026).

- [x] **T1 (FR-1, FR-2, FR-3, NFR-1):** setter de `SelectedItems` em `BaseLayoutPage.cs` com timer de 80 ms para toolbar e tamanho/contagem; item destacado, foco e rolagem imediatos. Pronto: build Release 0/0.
- [x] **T2 (FR-4, AC-3):** `FlushSelectionToToolbar()` aplica o pendente e para o timer; chamado antes de executar comandos. Pronto: Delete/Enter logo após a seta age sobre o item destacado (UI Automation).
- [x] **T3 (AC-1):** medir A-B-A, 5 rodadas, `Down` e `End`, e registrar em `docs/agents/evidence/F026/`. Pronto: tabela com mediana e CPU em `docs/agents/evidence/F026/relatorio.md`.
- [ ] **T4 (AC-2, AC-4):** verificação manual/UIA do estado final e de Ctrl+A/Shift+setas. (Deixado desmarcado: `relatorio.md` registra NOT TESTED para AC-2 e AC-4).
