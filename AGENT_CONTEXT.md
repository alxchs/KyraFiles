# AGENT_CONTEXT

Estado atual, curto. Atualize ao mudar de tarefa, ao parar e ao receber bloqueio. Detalhes ficam em `STATUS.md`, `DECISIONS.md` e `docs/agents/`.

```text
PROJECT        Files Custom (derivado do Files Community). Regras: MASTER_SPEC.md; continuidade: docs/agents/CONTINUITY.md
OBJECTIVE      File manager moderno com UX mais previsível. Ver MASTER_SPEC.md §2 e §44
CURRENT PHASE  F029 Setas no mínimo como no Explorer (em andamento; F028 entregue)
CURRENT TASK   T1/T3: medir Explorer e variantes (f029_ab.ps1) só com o Alexandre fora do computador
ACCEPTANCE     AC-1 a AC-5 de docs/specs/F029-setas-como-explorer/spec.md
REGRA DA MAIN  PROIBIDO alterar direto na main. AGY e Claude criam branches próprias (feature/<nome>) e integram via squash/rebase.
DO NOT CHANGE  Nada na main diretamente. Trabalho somente em branch de feature.
REGRA COLAB    A branch colab e custom/main espelham a main exclusivamente por fast-forward (--ff-only).
OWNER          Claude (assumiu a pedido do Alexandre em 21/10/2026)
CURRENT BRANCH main (F029 parcial integrada a pedido do Alexandre em 21/10; bandeiras A/B temporárias ainda no código, T4 pendente)
LAST DECISION  21/10/2026: ciclo "gerar exe novo e repetir testes" até o Alexandre aprovar; sem nova spec a menos que precise
NEXT ACTION    Medir com o Alexandre ausente; fixar a variante vencedora e remover as bandeiras (T4)
BLOCKER        Medição automática pausada: clique caiu em VS Code ("Accept All Changes") em 21/10 13:12; travas novas nos scripts
SIDE WORK      21/10: limpeza de builds velhos (arm64/Debug, sobras .locked no AppX, saída do mkfile, binários do upstream-base), 631 MB
F017 WIN+E     Concluído, empacotamento AppX completo corrigido e funcionando
LAST UPDATE    2026-10-21 - Claude
```
