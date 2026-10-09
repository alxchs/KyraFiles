# AGENT_CONTEXT

Estado atual, curto. Atualize ao mudar de tarefa, ao parar e ao receber bloqueio. Detalhes ficam em `STATUS.md`, `DECISIONS.md` e `docs/agents/`.

```text
PROJECT        Files Custom (derivado do Files Community). Regras: MASTER_SPEC.md; continuidade: docs/agents/CONTINUITY.md
OBJECTIVE      File manager moderno com UX mais previsível (rename, colunas, abas, menu, preview/details, busca). Ver MASTER_SPEC.md §2 e §44
CURRENT PHASE  G1 não fechado (revisão 08/10, branch claude/revisao-g1) + F012 em andamento
CURRENT TASK   F012 (gerenciador padrão com fallback) e diálogo de arquivos; ver docs/specs/F012-explorer-replacement/tasks.md
ACCEPTANCE     tasks.md da F012 aprovado (Claude, 09/10); T1, T2, T4 CONFIRMED; T3, T6, T7 com teste pendente
DO NOT CHANGE  Fora dos arquivos listados na D-015 e no plan.md da F012.
REGRA COLAB    A branch colab é estritamente origem e destino final de integração. Nunca desenvolver diretamente nela.
OWNER          Claude (pedido do Alexandre, AGY sem cota em 08/10); reserva por `ia claim`
CURRENT BRANCH feature/f012-explorer-replacement
LAST DECISION  D-015 (projetos C++ do upstream de volta via tools/Build-Native.ps1; launcher do upstream na F012)
NEXT ACTION    Alexandre responde ao Firewall; testar pela tela T3, T6 (Win+E com teclado real) e T7 (retorno do arquivo)
BLOCKER        Notificação do Firewall do Windows em primeiro plano impede a automação de UI
LAST UPDATE    2026-10-09 01:10 — Claude (handoff docs/agents/handoffs/2026-10-09_0110_Claude_F012-dialogo-de-arquivos.md)
```
