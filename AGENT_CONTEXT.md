# AGENT_CONTEXT

Estado atual, curto. Atualize ao mudar de tarefa, ao parar e ao receber bloqueio. Detalhes ficam em `STATUS.md`, `DECISIONS.md` e `docs/agents/`.

```text
PROJECT        Files Custom (derivado do Files Community). Regras: MASTER_SPEC.md; continuidade: docs/agents/CONTINUITY.md
OBJECTIVE      File manager moderno com UX mais previsível (rename, colunas, abas, menu, preview/details, busca). Ver MASTER_SPEC.md §2 e §44
CURRENT PHASE  G1 em refinamento + F014 (Sistema de Ajuda Contextual) implementado e validado fisicamente
CURRENT TASK   F014 concluída (T1..T6 validadas com evidências em docs/agents/evidence/f014/)
ACCEPTANCE     F014: HelpPane contextual, F1 routing, Badges editoriais, Settings HelpPage, build 0/0, UI confirmada.
DO NOT CHANGE  Fora dos arquivos da feature F014.
REGRA COLAB    A branch colab é estritamente origem e destino final de integração. Nunca desenvolver diretamente nela.
OWNER          AGY
CURRENT BRANCH feature/sistema-help-contextual
LAST DECISION  D-018 decidida: Sistema de Ajuda duplo (HelpPane lateral e HelpPage em Settings) com busca em tempo real e badges editoriais
NEXT ACTION    Aguardar confirmação do Alexandre para git push ou merge na branch de integração
BLOCKER        nenhum
LAST UPDATE    2026-10-10 23:18 — AGY
```
