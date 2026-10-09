# AGENT_CONTEXT

Estado atual, curto. Atualize ao mudar de tarefa, ao parar e ao receber bloqueio. Detalhes ficam em `STATUS.md`, `DECISIONS.md` e `docs/agents/`.

```text
PROJECT        Files Custom (derivado do Files Community). Regras: MASTER_SPEC.md; continuidade: docs/agents/CONTINUITY.md
OBJECTIVE      File manager moderno com UX mais previsível (rename, colunas, abas, menu, preview/details, busca). Ver MASTER_SPEC.md §2 e §44
CURRENT PHASE  G1 — Pipeline & Features OneCommander (F001..F011 concluídas com build 0/0)
CURRENT TASK   Elaboração da Spec F012 (Substituição Transparente do Explorer com Fallback)
ACCEPTANCE     G0 fechado; F001..F011 concluídas com build 0/0 e prova real no app
DO NOT CHANGE  Nenhum código fora de BaseGroupableLayoutPage, layouts e helper novo de nome/extensão (§13).
REGRA COLAB    A branch colab é estritamente origem e destino final de integração. Nunca desenvolver diretamente nela.
OWNER          AGY (fila do PLAYBOOK)
CURRENT BRANCH feature/f012-explorer-replacement
LAST DECISION  D-001..D-012 em DECISIONS.md; Aprovação da Opção A da F010 (06/10/2026)
NEXT ACTION    Aguardar aprovação da Spec F012 pelo Alexandre antes de gerar plan.md e tasks.md
BLOCKER        Nenhum
LAST UPDATE    2026-10-08 21:55 — AGY (Criada branch feature/f012-explorer-replacement a partir da colab para desenvolvimento da spec F012)
```
