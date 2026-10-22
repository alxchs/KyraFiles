# AGENT_CONTEXT

Estado atual, curto. Atualize ao mudar de tarefa, ao parar e ao receber bloqueio. Detalhes ficam em STATUS.md, DECISIONS.md e docs/agents/.

```text
PROJECT        Files Custom (derivado do Files Community). Regras: MASTER_SPEC.md; continuidade: docs/agents/CONTINUITY.md
OBJECTIVE      File manager moderno com UX mais previsível. Ver MASTER_SPEC.md §2 e §44
CURRENT PHASE  F031 Concluída e Integrada (Release v4.3.3)
CURRENT TASK   F031: Troca de abas (-59,1%) e nova aba (-45,3%) ultrarrápidas
ACCEPTANCE     AC-1, AC-2, AC-3 e NFR-1 validados e comprovados
REGRA DA MAIN  PROIBIDO alterar direto na main. AGY e Claude criam branches próprias (feature/<nome>) e integram via squash/rebase.
REGRA VERSAO   MANDATÓRIO (21/10/2026): O número da versão DEVE ser incrementado a CADA atualização integrada na main.
DO NOT CHANGE  Nada na main diretamente. Trabalho somente em branch de feature.
REGRA COLAB    A branch colab e custom/main espelham a main exclusivamente por fast-forward (--ff-only).
OWNER          AGY (F031)
CURRENT BRANCH main (v4.3.3)
LAST DECISION  22/10/2026: F031 validada com sucesso, instalador v4.3.3 compilado e testado
NEXT ACTION    Aguardar confirmação do Alexandre para git push
BLOCKER        Nenhum
LAST UPDATE    2026-10-22 - AGY (Conclusão F031 / Release v4.3.3)
```
