# AGENT_CONTEXT

Estado atual, curto. Atualize ao mudar de tarefa, ao parar e ao receber bloqueio. Detalhes ficam em STATUS.md, DECISIONS.md e docs/agents/.

`	ext
PROJECT        Files Custom (derivado do Files Community). Regras: MASTER_SPEC.md; continuidade: docs/agents/CONTINUITY.md
OBJECTIVE      File manager moderno com UX mais previsível. Ver MASTER_SPEC.md §2 e §44
CURRENT PHASE  Release v4.3.2 publicado na main (F030, F032, F033)
CURRENT TASK   v4.3.2 publicada; próximo passo: F031 (otimização de troca de abas)
ACCEPTANCE     ACs das specs F030 a F033
REGRA DA MAIN  PROIBIDO alterar direto na main. AGY e Claude criam branches próprias (feature/<nome>) e integram via squash/rebase.
REGRA VERSAO   MANDATÓRIO (21/10/2026): O número da versão DEVE ser incrementado a CADA atualização integrada na main.
DO NOT CHANGE  Nada na main diretamente. Trabalho somente em branch de feature.
REGRA COLAB    A branch colab e custom/main espelham a main exclusivamente por fast-forward (--ff-only).
OWNER          AGY / Claude (trabalho conjunto sob demanda)
CURRENT BRANCH main (v4.3.2)
LAST DECISION  21/10/2026: Incremento automático de versão a cada merge na main; v4.3.2 integrada via squash
NEXT ACTION    F031 (medição e otimização da velocidade de abertura/troca de abas)
BLOCKER        Nenhum
LAST UPDATE    2026-10-21 - AGY (Release v4.3.2 na main com squash)
`
