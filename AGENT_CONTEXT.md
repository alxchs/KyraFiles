# AGENT_CONTEXT

Estado atual, curto. Atualize ao mudar de tarefa, ao parar e ao receber bloqueio. Detalhes ficam em STATUS.md, DECISIONS.md e docs/agents/.

```text
PROJECT        Files Custom (derivado do Files Community). Regras: MASTER_SPEC.md; continuidade: docs/agents/CONTINUITY.md
OBJECTIVE      File manager moderno com UX mais previsível. Ver MASTER_SPEC.md §2 e §44
CURRENT PHASE  Release v4.3.7 (correção da F035) publicada
CURRENT TASK   F035: segunda instância; próximos: crash do GridLayoutPage, AC-5, retrabalho de evidências
ACCEPTANCE     Build 0/0, Gate F021 aprovado, 20/20 rodadas sem crash, instalador 4.3.7 instalado
REGRA DA MAIN  PROIBIDO alterar direto na main. AGY e Claude criam branches próprias (feature/<nome>) e integram via squash/rebase.
REGRA VERSAO   MANDATÓRIO (21/10/2026): O número da versão DEVE ser incrementado a CADA atualização integrada na main.
REGRA PERF     MANDATÓRIO (24/10/2026): NENHUMA alteração deve ser aceita se onerar a performance; analisar e relatar ganhos.
DO NOT CHANGE  Nada na main diretamente. Trabalho somente em branch de feature.
REGRA COLAB    A branch colab e custom/main espelham a main exclusivamente por fast-forward (--ff-only).
OWNER          Claude (revisor e publicação); AGY (execução)
CURRENT BRANCH main (v4.3.7)
LAST DECISION  26/10/2026: v4.3.7 só com a correção da F035; Alexandre autorizou preparar e publicar
NEXT ACTION    Reproduzir o crash do GridLayoutPage (sem alterar src); retrabalho das evidências em janela de AWAY
BLOCKER        Nenhum
LAST UPDATE    2026-10-26 - Claude (Release v4.3.7)
```
