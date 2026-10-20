# AGENT_CONTEXT

Estado atual, curto. Atualize ao mudar de tarefa, ao parar e ao receber bloqueio. Detalhes ficam em `STATUS.md`, `DECISIONS.md` e `docs/agents/`.

```text
PROJECT        Files Custom (derivado do Files Community). Regras: MASTER_SPEC.md; continuidade: docs/agents/CONTINUITY.md
OBJECTIVE      File manager moderno com UX mais previsível. Ver MASTER_SPEC.md §2 e §44
CURRENT PHASE  F025 Baseline do upstream e trace (so medicao; reabre o diagnostico da F024, D-023)
CURRENT TASK   Brief docs/agents/tasks/F025-baseline-upstream-e-trace.md (P1 a P6), a executar pela AGY
ACCEPTANCE     Criterio de aceite da secao 5 do brief F025
REGRA DA MAIN  PROIBIDO alterar direto na main. AGY e Claude criam branches próprias (feature/<nome>) e integram via squash/rebase.
DO NOT CHANGE  Nada na main diretamente. Trabalho somente em branch de feature.
REGRA COLAB    A branch colab e custom/main espelham a main exclusivamente por fast-forward (--ff-only).
OWNER          Antigravity (medicao); Claude (revisao)
CURRENT BRANCH feature/f025-diagnostico-baseline-upstream
LAST DECISION  D-023 (20/10/2026): veredito da F024 e INFERRED, sem prova; medir upstream puro e trace antes de decidir. Diretriz "nao insistir" revogada.
NEXT ACTION    AGY executa P1 a P6 sem alterar src/; Claude revisa o relatorio; Alexandre decide continuar ou encerrar
BLOCKER        Nenhum. Pendente: decisao do Alexandre apos o relatorio F025
SIDE WORK      Laboratório C:\FilesUXLab limpo e artefatos de teste removidos
F017 WIN+E     Concluído, empacotamento AppX completo corrigido e funcionando
LAST UPDATE    2026-10-20 - Claude (revisao da F024, D-023, brief F025, licoes em INSTRUCTIONS_OPERARY.md)
```
