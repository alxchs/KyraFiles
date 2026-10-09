# AGENT_CONTEXT

Estado atual, curto. Atualize ao mudar de tarefa, ao parar e ao receber bloqueio. Detalhes ficam em `STATUS.md`, `DECISIONS.md` e `docs/agents/`.

```text
PROJECT        Files Custom (derivado do Files Community). Regras: MASTER_SPEC.md; continuidade: docs/agents/CONTINUITY.md
OBJECTIVE      File manager moderno com UX mais previsível (rename, colunas, abas, menu, preview/details, busca). Ver MASTER_SPEC.md §2 e §44
CURRENT PHASE  G1 não fechado (revisão 08/10, branch claude/revisao-g1) + F012 em andamento
CURRENT TASK   F012 restante (AC-3 e AC-10 a testar, AC-9, T8); G1 dividido em 2 frentes da AGY (brief em agy/correcoes-g1)
ACCEPTANCE     F012: T1..T5, T7, T9 CONFIRMED; AC-3 navegador, AC-9, AC-10, T8 pendentes. F013: AC-1/3/4 CONFIRMED
DO NOT CHANGE  Fora dos arquivos listados na D-015, na D-016 e no plan.md da F012.
REGRA COLAB    A branch colab é estritamente origem e destino final de integração. Nunca desenvolver diretamente nela.
OWNER          Claude (pedido do Alexandre, AGY sem cota em 08/10); reserva por `ia claim`
CURRENT BRANCH feature/f012-explorer-replacement
LAST DECISION  D-017 decidida: redirecionar janelas do Explorer com o Files em execução (sem administrador)
NEXT ACTION    Alexandre: disparar AGY frentes A e B (comandos no chat de 09/10) e concluir os 2 diálogos Salvar abertos; Claude: registrar pacote e testar 91d244812 (Program.cs), AC-3, AC-10
BLOCKER        disparo da AGY bloqueado pelo classificador (precisa do Alexandre); 2 diálogos Salvar do Files pendentes impedem registrar o pacote
LAST UPDATE    2026-10-09 15:45 — Claude
```
