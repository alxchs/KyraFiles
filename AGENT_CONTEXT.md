# AGENT_CONTEXT

Estado atual, curto. Atualize ao mudar de tarefa, ao parar e ao receber bloqueio. Detalhes ficam em `STATUS.md`, `DECISIONS.md` e `docs/agents/`.

```text
PROJECT        Files Custom (derivado do Files Community). Regras: MASTER_SPEC.md; continuidade: docs/agents/CONTINUITY.md
OBJECTIVE      File manager moderno com UX mais previsível. Ver MASTER_SPEC.md §2 e §44
CURRENT PHASE  F024 Choque de Performance e Navegação Fluida (Encerrado / Diagnóstico Final)
CURRENT TASK   Diagnóstico de Performance e Avaliação de Viabilidade registrado em docs/agents/DIAGNOSTICO_PERFORMANCE_E_VIABILIDADE.md
ACCEPTANCE     docs/specs/F024-choque-performance-navegacao/spec.md
REGRA DA MAIN  PROIBIDO alterar direto na main. AGY e Claude criam branches próprias (feature/<nome>) e integram via squash/rebase.
DO NOT CHANGE  Nada na main diretamente. Trabalho somente em branch de feature.
REGRA COLAB    A branch colab e custom/main espelham a main exclusivamente por fast-forward (--ff-only).
OWNER          Antigravity
CURRENT BRANCH feature/f024-choque-performance-navegacao
LAST DECISION  Diagnóstico consolidado com recomendação de cancelamento/encerramento devido a limitações congênitas do WinUI 3 (1.068s CPU, 1.041 MB RAM medidos em pasta de 1.600 itens).
NEXT ACTION    Aguardar decisão formal do Alexandre sobre encerramento do repositório/desenvolvimento
BLOCKER        Limitações de arquitetura do framework WinUI 3 / XAML Desktop
SIDE WORK      Laboratório C:\FilesUXLab limpo e artefatos de teste removidos
F017 WIN+E     Concluído, empacotamento AppX completo corrigido e funcionando
LAST UPDATE    2026-10-19 22:15 — AGY (Diagnóstico de performance e viabilidade concluído)
```
