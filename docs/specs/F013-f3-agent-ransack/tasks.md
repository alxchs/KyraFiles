# F013 — Tarefas

Status: **APROVADAS** (Claude, 09/10/2026). Executor: Claude, a pedido do Alexandre.
Branch: `feature/f012-explorer-replacement` (feature pequena, entregue junto com a F012 a pedido do Alexandre para usar a cota do dia).

- [x] **T1 — Ação F3** (`SearchWithAgentRansackAction.cs`, `SearchAction.cs`, `AgentRansackSearchProvider.cs`, `Resources.resw` en-US e pt-BR)
  - **Atende:** FR-001 a FR-004, AC-1 a AC-5.
  - **Pronto quando:** build 0/0; F3 abre o Agent Ransack na pasta; Ctrl+F abre a busca do KyraFiles.
- [x] **T2 — Teste pela tela** (`tools/perf/f013_f3_agent_ransack.ps1`)
  - **Atende:** AC-1, AC-3, AC-4, AC-5.
  - **Pronto quando:** saída do script e capturas em `docs/agents/evidence/f013/`.

## Estado em 09/10/2026 10:20 (Claude)

Build `mkfile r Files.slnx` = 0 Warning(s), 0 Error(s). Teste: `pwsh tools\perf\f013_f3_agent_ransack.ps1 -Capture` (teclas por `SendInput`).

| AC | Estado | Evidência |
|---|---|---|
| AC-1 | CONFIRMED | F3 numa pasta com espaço e acento: Agent Ransack abriu em 2,0 s com o título "Search: <pasta>\" |
| AC-2 | INFERRED | pelo código (`GetExecutablePath` nulo cai na busca do KyraFiles); não testado (exigiria remover o Agent Ransack) |
| AC-3 | CONFIRMED | F3 em "Início": busca do KyraFiles com foco, Agent Ransack não abriu |
| AC-4 | CONFIRMED | Ctrl+F: busca do KyraFiles com foco (captura `docs/agents/evidence/f013/01_ctrl_f_busca_files.png`) |
| AC-5 | INFERRED | textos em `Strings/en-US` e `Strings/pt-BR`; paleta de comandos não capturada |

Observado no teste: tocar Alt sozinho antes do F3 liga as teclas de acesso da barra de menus clássica (F004), e o F3 seguinte é engolido. É o comportamento normal de teclas de acesso; o script sai desse modo com Esc.
