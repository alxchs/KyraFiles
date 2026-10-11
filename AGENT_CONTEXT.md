# AGENT_CONTEXT

Estado atual, curto. Atualize ao mudar de tarefa, ao parar e ao receber bloqueio. Detalhes ficam em `STATUS.md`, `DECISIONS.md` e `docs/agents/`.

```text
PROJECT        Files Custom (derivado do Files Community). Regras: MASTER_SPEC.md; continuidade: docs/agents/CONTINUITY.md
OBJECTIVE      File manager moderno com UX mais previsível (rename, colunas, abas, menu, preview/details, busca). Ver MASTER_SPEC.md §2 e §44
CURRENT PHASE  F015 (índice de busca próprio): spec, plan e tasks APROVADOS em 11/10/2026
CURRENT TASK   F015 T1 (como instalar o serviço) e T2 (medições antes de construir)
ACCEPTANCE     docs/specs/F015-indice-de-busca/tasks.md, coluna "Pronto quando"
DO NOT CHANGE  Fora dos arquivos listados no plan.md da F015
REGRA COLAB    A branch colab é estritamente origem e destino final de integração. Nunca desenvolver diretamente nela.
OWNER          AGY (execução); Claude revisa
CURRENT BRANCH feature/f015-indice-de-busca
LAST DECISION  D-019: índice próprio em dois modos, sem duplicatas de subst/links; rede fora; link físico uma vez no local original
NEXT ACTION    Redisparar a AGY na conversa 651efcc3-970d-4b6d-9bcd-a7af6537fa17 (claude-sonnet-4-6) após 07:49 de 11/10. Trabalho parcial dela NÃO commitado: tools/spikes/, tools/perf/f015_*.ps1, DECISIONS.md
BLOCKER        Cota AGY: grupos claude/gpt até 11/10 07:49; gemini até 12/10 22:05. Trocar perfil não muda a conta do agy --print (contadores idênticos)
LAST UPDATE    2026-10-11 03:08 — Claude
```
