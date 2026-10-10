# AGENT_CONTEXT

Estado atual, curto. Atualize ao mudar de tarefa, ao parar e ao receber bloqueio. Detalhes ficam em `STATUS.md`, `DECISIONS.md` e `docs/agents/`.

```text
PROJECT        Files Custom (derivado do Files Community). Regras: MASTER_SPEC.md; continuidade: docs/agents/CONTINUITY.md
OBJECTIVE      File manager moderno com UX mais previsível (rename, colunas, abas, menu, preview/details, busca). Ver MASTER_SPEC.md §2 e §44
CURRENT PHASE  G1 não fechado (revisão 08/10, branch claude/revisao-g1) + F012 concluída e integrada
CURRENT TASK   F012 concluída (T1..T9 validadas); G1 dividido em 2 frentes da AGY (brief em agy/correcoes-g1)
ACCEPTANCE     F012: T1..T9 CONFIRMED. F013: AC-1/3/4 CONFIRMED. Build Release x64 0/0.
DO NOT CHANGE  Fora dos arquivos listados na D-015, na D-016 e no plan.md da F012.
REGRA COLAB    A branch colab é estritamente origem e destino final de integração. Nunca desenvolver diretamente nela.
OWNER          AGY / Claude
CURRENT BRANCH feature/f012-explorer-replacement (integrada em colab)
LAST DECISION  D-017 decidida: redirecionar janelas do Explorer com o Files em execução (sem administrador)
NEXT ACTION    Aguardar confirmação do Alexandre para git push na origin; prosseguir com as frentes do G1
BLOCKER        nenhum
LAST UPDATE    2026-10-10 08:15 — AGY
```
