# AGENT_CONTEXT

Estado atual, curto. Atualize ao mudar de tarefa, ao parar e ao receber bloqueio. Detalhes ficam em STATUS.md, DECISIONS.md e docs/agents/.

```text
PROJECT        Files Custom (derivado do Files Community). Regras: MASTER_SPEC.md; continuidade: docs/agents/CONTINUITY.md
OBJECTIVE      File manager moderno com UX mais previsível. Ver MASTER_SPEC.md §2 e §44
CURRENT PHASE  Release v4.3.4 Concluída (Limpeza de Desenvolvimento e Instalador Consolidado)
CURRENT TASK   r434: Limpeza do ambiente de desenvolvimento e geração da v4.3.4
ACCEPTANCE     Etapas 1, 2 e 3 100% concluídas, Gate F021 aprovado, Instalador 4.3.4 gerado e validado
REGRA DA MAIN  PROIBIDO alterar direto na main. AGY e Claude criam branches próprias (feature/<nome>) e integram via squash/rebase.
REGRA VERSAO   MANDATÓRIO (21/10/2026): O número da versão DEVE ser incrementado a CADA atualização integrada na main.
DO NOT CHANGE  Nada na main diretamente. Trabalho somente em branch de feature.
REGRA COLAB    A branch colab e custom/main espelham a main exclusivamente por fast-forward (--ff-only).
OWNER          AGY (r434)
CURRENT BRANCH main (v4.3.4)
LAST DECISION  22/10/2026: Limpeza concluída, instalador v4.3.4 compilado, instalado e validado localmente
NEXT ACTION    Claude revisa e publica a release 4.3.4 no GitHub
BLOCKER        Nenhum
LAST UPDATE    2026-10-22 - AGY (Conclusão r434 / Release v4.3.4)
```
