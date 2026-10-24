# AGENT_CONTEXT

Estado atual, curto. Atualize ao mudar de tarefa, ao parar e ao receber bloqueio. Detalhes ficam em STATUS.md, DECISIONS.md e docs/agents/.

```text
PROJECT        Files Custom (derivado do Files Community). Regras: MASTER_SPEC.md; continuidade: docs/agents/CONTINUITY.md
OBJECTIVE      File manager moderno com UX mais previsível. Ver MASTER_SPEC.md §2 e §44
CURRENT PHASE  Release v4.3.6 Concluída e Pronta para Publicação
CURRENT TASK   r436: Integração dos 4 tópicos (T1 evidências, T2 F034 sem checkbox, T3 F029 setas Explorer, T4 F023 estresse/Omnibar crash fix/lições Everything), instalador 4.3.6 e validação geral
ACCEPTANCE     Compilação C++ e C# 0/0, Gate F021 aprovado, Instalador 4.3.6 validado, sem vazamento de handles, sem crash no Omnibar
REGRA DA MAIN  PROIBIDO alterar direto na main. AGY e Claude criam branches próprias (feature/<nome>) e integram via squash/rebase.
REGRA VERSAO   MANDATÓRIO (21/10/2026): O número da versão DEVE ser incrementado a CADA atualização integrada na main.
REGRA PERF     MANDATÓRIO (24/10/2026): NENHUMA alteração deve ser aceita se onerar a performance; analisar e relatar ganhos.
DO NOT CHANGE  Nada na main diretamente. Trabalho somente em branch de feature.
REGRA COLAB    A branch colab e custom/main espelham a main exclusivamente por fast-forward (--ff-only).
OWNER          AGY (r436)
CURRENT BRANCH main (v4.3.6)
LAST DECISION  24/10/2026: Conclusão e integração dos 4 tópicos em branches independentes e versão 4.3.6
NEXT ACTION    Publicação da release v4.3.6 no GitHub
BLOCKER        Nenhum
LAST UPDATE    2026-10-24 - AGY (Release v4.3.6)
```
