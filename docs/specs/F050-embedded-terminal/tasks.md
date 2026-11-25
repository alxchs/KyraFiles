# F050 — Terminal embutido no rodapé — TASKS

Status: RASCUNHO (aguardando aprovação; T0 é gate de decisão antes da implementação).

Regras: seguir `plan.md`; build Release x64 sem erros; **não avançar para T1 sem aprovar o resultado de T0** (`spec.md §8`); manter gate F021 e `check-legacy-names.ps1` verdes.

- [ ] **T0 — Medir custo do componente (ConPTY + renderizador) e decidir.** Prototipar e medir RAM, tempo de abertura (meta ≤ 1 s) e impacto no instalador; registrar em `docs/agents/evidence/F050/cost-measurement.md`. Se custo for alto, propor adiamento; se aprovado, seguir para T1. Atende: ponto em aberto §8, NFR-001.
- [ ] **T1 — Sessão ConPTY com Job Object (`KILL_ON_JOB_CLOSE`).** Implementar `ConPtySession` via CsWin32 garantindo encerramento limpo e zero processos órfãos ao fechar aba ou app, e detecção de comando em execução. Atende: FR-003, AC-3, AC-5.
- [ ] **T2 — Controle visual no rodapé de `ShellPanesPage`.** Criar `EmbeddedTerminalViewModel` e `EmbeddedTerminalControl` com reflow ao redimensionar, isolamento de atalhos de teclado (FR-002), seleção de shell (PowerShell/cmd/WSL/Git Bash) e ação "Colar caminho". Atende: US1, US3, AC-1, AC-4, FR-001, FR-002.
- [ ] **T3 — Acompanhamento de pasta, casos de borda e fechamento.** Implementar "Acompanhar pasta" (sem interromper comando ativo), fallback para UNC/caminho longo (AC-6), validação dos ACs 1–7, gate F021, `check-legacy-names.ps1`, `STATUS.md` e `AGENT_CONTEXT.md`. Atende: US2, AC-2, AC-6, AC-7.
