# AGENT_CONTEXT

Estado atual, curto. Atualize ao mudar de tarefa, ao parar e ao receber bloqueio. Detalhes ficam em `STATUS.md`, `DECISIONS.md` e `docs/agents/`.

```text
PROJECT        Files Custom (derivado do Files Community). Regras: MASTER_SPEC.md; continuidade: docs/agents/CONTINUITY.md
OBJECTIVE      File manager moderno com UX mais previsível (rename, colunas, abas, menu, preview/details, busca). Ver MASTER_SPEC.md §2 e §44
CURRENT PHASE  F020 Concluída / Transição para próxima feature
CURRENT TASK   Baseline consolidada (commit único bd7c9c678). Pronto para próxima feature em branch dedicada.
ACCEPTANCE     docs/specs/F020-aba-unica-por-endereco-e-posicao-settings/tasks.md concluída; instalador testado
REGRA DA MAIN  PROIBIDO alterar direto na main. AGY e Claude criam branches próprias (feature/<nome>) e integram via squash/rebase.
DO NOT CHANGE  Nada na main diretamente. Trabalho somente em branch de feature.
REGRA COLAB    A branch colab e custom/main espelham a main exclusivamente por fast-forward (--ff-only).
OWNER          Antigravity
CURRENT BRANCH main (baseline limpa pronta)
LAST DECISION  D-024: Squash com Rebase obrigatório para integração na main; proibido commit direto na main
NEXT ACTION    Aguardar confirmação de push forçado do baseline ou definição da próxima feature (F021)
BLOCKER        Nenhum
SIDE WORK      Próxima feature a ser criada em branch feature/<nome>
F017 WIN+E     Concluído, empacotamento AppX completo corrigido e funcionando
LAST UPDATE    2026-10-15 — Antigravity (Baseline consolidado em 1 commit, regra de branches estrita)
```
