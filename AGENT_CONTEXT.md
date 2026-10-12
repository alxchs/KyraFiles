# AGENT_CONTEXT

Estado atual, curto. Atualize ao mudar de tarefa, ao parar e ao receber bloqueio. Detalhes ficam em `STATUS.md`, `DECISIONS.md` e `docs/agents/`.

```text
PROJECT        Files Custom (derivado do Files Community). Regras: MASTER_SPEC.md; continuidade: docs/agents/CONTINUITY.md
OBJECTIVE      File manager moderno com UX mais previsível (rename, colunas, abas, menu, preview/details, busca). Ver MASTER_SPEC.md §2 e §44
CURRENT PHASE  F015 (índice de busca próprio): spec, plan e tasks APROVADOS em 11/10/2026
CURRENT TASK   F015 T7 (DirectoryScanner e HardLinkResolver)
ACCEPTANCE     docs/specs/F015-indice-de-busca/tasks.md, coluna "Pronto quando"
DO NOT CHANGE  Fora dos arquivos listados no plan.md da F015
REGRA COLAB    A branch colab é estritamente origem e destino final de integração. Nunca desenvolver diretamente nela.
OWNER          Antigravity (execução autônoma T3-T12); Alexandre para cliques UAC (T1, T2a)
CURRENT BRANCH feature/f015-indice-de-busca
LAST DECISION  D-019: índice próprio em dois modos; D-020 (PENDING teste elevado)
NEXT ACTION    Executar T7; Alexandre aciona UAC para T1 e T2a quando oportuno
BLOCKER        Nenhum para T3-T12 (modo básico não requer admin)
SIDE WORK      Formas de controle/aba (ControlShapeStyle, 10 modelos), fora do escopo da F015, sem spec: fechada em fbf052335 (build 0/0; abre e não cai nas 10 formas; aparência de cada forma NOT TESTED). Pendente: spec retroativa ou mover para branch própria antes de integrar
LAST UPDATE    2026-10-12 — Claude (revisão do pendente da AGY)
```
