# AGENT_CONTEXT

Estado atual, curto. Atualize ao mudar de tarefa, ao parar e ao receber bloqueio. Detalhes ficam em `STATUS.md`, `DECISIONS.md` e `docs/agents/`.

```text
PROJECT        Files Custom (derivado do Files Community). Regras: MASTER_SPEC.md; continuidade: docs/agents/CONTINUITY.md
OBJECTIVE      File manager moderno com UX mais previsível (rename, colunas, abas, menu, preview/details, busca). Ver MASTER_SPEC.md §2 e §44
CURRENT PHASE  F024 Choque de Performance e Navegação Fluida (Última Tentativa)
CURRENT TASK   SDD da F024: spec, plan, tasks e registro de artefatos de teste
ACCEPTANCE     docs/specs/F024-choque-performance-navegacao/spec.md
REGRA DA MAIN  PROIBIDO alterar direto na main. AGY e Claude criam branches próprias (feature/<nome>) e integram via squash/rebase.
DO NOT CHANGE  Nada na main diretamente. Trabalho somente em branch de feature.
REGRA COLAB    A branch colab e custom/main espelham a main exclusivamente por fast-forward (--ff-only).
OWNER          Antigravity
CURRENT BRANCH feature/f024-choque-performance-navegacao
LAST DECISION  Opção B: Cirurgia de choque de performance e bateria de testes para decisão final
NEXT ACTION    Aprovação do tasks.md pelo Alexandre para início da execução da T1
BLOCKER        Aguardando aprovação do plano/tarefas pelo Alexandre
SIDE WORK      Higiene estrita de artefatos de teste em test_artifacts_registry.md
F017 WIN+E     Concluído, empacotamento AppX completo corrigido e funcionando
LAST UPDATE    2026-10-19 — AGY (Especificação SDD F024 concluída)
```

