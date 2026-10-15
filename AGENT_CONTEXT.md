# AGENT_CONTEXT

Estado atual, curto. Atualize ao mudar de tarefa, ao parar e ao receber bloqueio. Detalhes ficam em `STATUS.md`, `DECISIONS.md` e `docs/agents/`.

```text
PROJECT        Files Custom (derivado do Files Community). Regras: MASTER_SPEC.md; continuidade: docs/agents/CONTINUITY.md
OBJECTIVE      File manager moderno com UX mais previsível (rename, colunas, abas, menu, preview/details, busca). Ver MASTER_SPEC.md §2 e §44
CURRENT PHASE  F020 (Aba única por endereço e Configurações na primeira posição)
CURRENT TASK   F020 Concluída (T1–T6) — commit e push na branch de trabalho
ACCEPTANCE     docs/specs/F020-aba-unica-por-endereco-e-posicao-settings/tasks.md, coluna "Pronto quando"
DO NOT CHANGE  Fora dos arquivos listados no plan.md da F020
REGRA COLAB    A branch colab é estritamente origem e destino final de integração. Nunca desenvolver diretamente nela.
OWNER          Antigravity
CURRENT BRANCH feature/f020-aba-unica-config-primeira-posicao
LAST DECISION  D-023: F020 especificação de aba única e Settings na posição 0
NEXT ACTION    Realizar commit e push de feature/f020-aba-unica-config-primeira-posicao e prosseguir para próximo passo
BLOCKER        Nenhum (autorizado pelo Alexandre em 14/10/2026)
SIDE WORK      F015 (Modo Completo MFT) e F019 (10 PNGs de aprovação visual) aguardando slots
F017 WIN+E     T1–T5 concluídas; instalador Inno Setup isolou dependências quebrando hostpolicy do .NET (requer ajuste)
LAST UPDATE    2026-10-15 — Antigravity (F020 T1–T6 concluídas, build release 0/0)
```
