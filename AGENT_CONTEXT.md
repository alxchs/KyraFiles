# AGENT_CONTEXT

Estado atual, curto. Atualize ao mudar de tarefa, ao parar e ao receber bloqueio. Detalhes ficam em `STATUS.md`, `DECISIONS.md` e `docs/agents/`.

```text
PROJECT        Files Custom (derivado do Files Community). Regras: MASTER_SPEC.md; continuidade: docs/agents/CONTINUITY.md
OBJECTIVE      File manager moderno com UX mais previsível (rename, colunas, abas, menu, preview/details, busca). Ver MASTER_SPEC.md §2 e §44
CURRENT PHASE  G1 não fechado (revisão 08/10, branch claude/revisao-g1) + F012 em andamento
CURRENT TASK   F012 (gerenciador padrão com fallback) e diálogo de arquivos; ver docs/specs/F012-explorer-replacement/tasks.md
ACCEPTANCE     tasks.md da F012 aprovado (Claude, 09/10); T1..T5 e T7 CONFIRMED; T6: Win+E NOT TESTED, explorer.exe direto pendente (D-017)
DO NOT CHANGE  Fora dos arquivos listados na D-015, na D-016 e no plan.md da F012.
REGRA COLAB    A branch colab é estritamente origem e destino final de integração. Nunca desenvolver diretamente nela.
OWNER          Claude (pedido do Alexandre, AGY sem cota em 08/10); reserva por `ia claim`
CURRENT BRANCH feature/f012-explorer-replacement
LAST DECISION  D-016 (diálogo de arquivos isolado da sessão do usuário); D-017 PENDING DECISION (explorer.exe <pasta>)
NEXT ACTION    Alexandre: decidir D-017 e testar Win+E com teclado real; depois AC-3 (/select), AC-10 e T8
BLOCKER        nenhum
LAST UPDATE    2026-10-09 07:30 — Claude (handoff docs/agents/handoffs/2026-10-09_0730_Claude_F012-dialogo-devolve-arquivo.md)
```
