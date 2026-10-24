# AGENT_CONTEXT

Estado atual, curto. Atualize ao mudar de tarefa, ao parar e ao receber bloqueio. Detalhes ficam em STATUS.md, DECISIONS.md e docs/agents/.

```text
PROJECT        Files Custom (derivado do Files Community). Regras: MASTER_SPEC.md; continuidade: docs/agents/CONTINUITY.md
OBJECTIVE      File manager moderno com UX mais previsível. Ver MASTER_SPEC.md §2 e §44
CURRENT PHASE  Release v4.3.5 Concluída e Publicada (Recomposição de Pacote e Instalador Consolidado)
CURRENT TASK   r435: Recomposição de pacote, validação de integridade, instalador 4.3.5 e publicação
ACCEPTANCE     Compilação C++ e C# 0/0, Gate F021 aprovado, Instalador 4.3.5 validado e publicado
REGRA DA MAIN  PROIBIDO alterar direto na main. AGY e Claude criam branches próprias (feature/<nome>) e integram via squash/rebase.
REGRA VERSAO   MANDATÓRIO (21/10/2026): O número da versão DEVE ser incrementado a CADA atualização integrada na main.
DO NOT CHANGE  Nada na main diretamente. Trabalho somente em branch de feature.
REGRA COLAB    A branch colab e custom/main espelham a main exclusivamente por fast-forward (--ff-only).
OWNER          AGY (r435)
CURRENT BRANCH main (v4.3.5)
LAST DECISION  24/10/2026: Recomposição e publicação da v4.3.5 autorizada pelo Alexandre
NEXT ACTION    Claude dá continuidade às tarefas F029, F030 T2, F032 T4, F033 T4 e F034
BLOCKER        Nenhum
LAST UPDATE    2026-10-24 - AGY (Publicação v4.3.5)
```
