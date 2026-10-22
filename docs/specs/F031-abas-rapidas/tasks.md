# F031 — Tarefas

Status: **CONCLUÍDAS** (AGY, 22/10/2026).

- [x] **T1 (AC-1):** `tools/perf/f031_tabs.ps1` e linha de base Files × Explorer. Pronto: CSV e tabela em `docs/agents/evidence/F031/` (SwitchTab mediana: 1.116,7 ms, NewTab mediana: 526,4 ms).
- [x] **T2:** trace da troca e da aba nova; hipóteses principais identificadas (Delay de 100ms artificial em MainPage, I/O síncrono redundante de título e carga acoplada); plano atualizado.
- [x] **T3 (FR-1 a FR-3):** mudança de código das hipóteses identificadas. Foco sem delay e fast-path de título em `NavigationHelpers.cs` e `MainPage.xaml.cs`. Pronto: build 0/0.
- [x] **T4 (AC-2, AC-3, NFR-1):** medição depois (SwitchTab -59,1%, NewTab -45,3%), memória com 10 abas estável (crescimento < 0,1%), conferência visual de seleção e rolagem registrada (`tab10_proof.png`), nota de liberação pronta.
