# AGENT_CONTEXT

Estado atual, curto. Atualize ao mudar de tarefa, ao parar e ao receber bloqueio. Detalhes ficam em `STATUS.md`, `DECISIONS.md` e `docs/agents/`.

```text
PROJECT        Files Custom (derivado do Files Community). Regras: MASTER_SPEC.md; continuidade: docs/agents/CONTINUITY.md
OBJECTIVE      File manager moderno com UX mais previsível (rename, colunas, abas, menu, preview/details, busca). Ver MASTER_SPEC.md §2 e §44
CURRENT PHASE  F015 Índice de busca: T17 (medições finais) em andamento; correções T17a a T17e aprovadas
CURRENT TASK   T17a (passadas lineares no Reconciler/MarkDeleted), depois T17b, T17c, T17d, T17e; então refazer a T17
ACCEPTANCE     linhas T17a..T17e de docs/specs/F015-indice-de-busca/tasks.md; diagnóstico em tools/perf/README.md ("F015 T17 — latência com snapshot")
REGRA DA MAIN  PROIBIDO alterar direto na main. AGY e Claude criam branches próprias (feature/<nome>) e integram via squash/rebase.
DO NOT CHANGE  Nada na main diretamente. Trabalho somente em branch de feature.
REGRA COLAB    A branch colab e custom/main espelham a main exclusivamente por fast-forward (--ff-only).
OWNER          Antigravity (T17a..T17e); Claude revisa
CURRENT BRANCH feature/f015-medicoes-finais
LAST DECISION  D-024: Squash com Rebase obrigatório para integração na main; proibido commit direto na main
NEXT ACTION    AGY executa T17a (teste comparando com o algoritmo antigo + microbenchmark de 2 M itens)
BLOCKER        Nenhum. AC-20 NOT TESTED até haver es.exe (autorização do Alexandre)
SIDE WORK      Próxima feature a ser criada em branch feature/<nome>
F017 WIN+E     Concluído, empacotamento AppX completo corrigido e funcionando
LAST UPDATE    2026-10-16 — Claude (medição T17 válida, perfil: reconciliação do C: nunca termina; T17a..T17e criadas)
```
