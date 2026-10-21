# AGENT_CONTEXT

Estado atual, curto. Atualize ao mudar de tarefa, ao parar e ao receber bloqueio. Detalhes ficam em `STATUS.md`, `DECISIONS.md` e `docs/agents/`.

```text
PROJECT        Files Custom (derivado do Files Community). Regras: MASTER_SPEC.md; continuidade: docs/agents/CONTINUITY.md
OBJECTIVE      File manager moderno com UX mais previsível. Ver MASTER_SPEC.md §2 e §44
CURRENT PHASE  F028 Custo por passo da seleção (3ª revisão: PgUp/PgDn sem contorno sobre o cabeçalho)
CURRENT TASK   Alexandre testa o build de 21/10 09:20 (docs/releases/2026-10-21_0920_build-f028-pgup-pgdn-cabecalho.md)
ACCEPTANCE     AC-6 da spec F028; sensação de fluidez confirmada pelo Alexandre
REGRA DA MAIN  PROIBIDO alterar direto na main. AGY e Claude criam branches próprias (feature/<nome>) e integram via squash/rebase.
DO NOT CHANGE  Nada na main diretamente. Trabalho somente em branch de feature.
REGRA COLAB    A branch colab e custom/main espelham a main exclusivamente por fast-forward (--ff-only).
OWNER          Claude (assumiu a pedido do Alexandre em 21/10/2026)
CURRENT BRANCH main (F024 a F028 consolidadas); próxima feature em branch nova
LAST DECISION  21/10/2026: ciclo "gerar exe novo e repetir testes" até o Alexandre aprovar; sem nova spec a menos que precise
NEXT ACTION    Resultado do teste do Alexandre; depois, spec do próximo alvo (peso da linha nas setas; cache de propriedades opcional)
BLOCKER        Nenhum
SIDE WORK      21/10: limpeza de builds velhos (arm64/Debug, sobras .locked no AppX, saída do mkfile, binários do upstream-base), 631 MB
F017 WIN+E     Concluído, empacotamento AppX completo corrigido e funcionando
LAST UPDATE    2026-10-21 - Claude
```
