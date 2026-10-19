# AGENT_CONTEXT

Estado atual, curto. Atualize ao mudar de tarefa, ao parar e ao receber bloqueio. Detalhes ficam em `STATUS.md`, `DECISIONS.md` e `docs/agents/`.

```text
PROJECT        Files Custom (derivado do Files Community). Regras: MASTER_SPEC.md; continuidade: docs/agents/CONTINUITY.md
OBJECTIVE      File manager moderno com UX mais previsível (rename, colunas, abas, menu, preview/details, busca). Ver MASTER_SPEC.md §2 e §44
CURRENT PHASE  F023 Busca Rápida Híbrida e Release v4.3.0
CURRENT TASK   Build de Release v4.3.0, geração do instalador e integração na main
ACCEPTANCE     docs/releases/2026-10-18_2210_build-FilesCustomSetup-4.3.0.md; verify_no_index_service.ps1 (validado)
REGRA DA MAIN  PROIBIDO alterar direto na main. AGY e Claude criam branches próprias (feature/<nome>) e integram via squash/rebase.
DO NOT CHANGE  Nada na main diretamente. Trabalho somente em branch de feature.
REGRA COLAB    A branch colab e custom/main espelham a main exclusivamente por fast-forward (--ff-only).
OWNER          Antigravity (desenvolvimento e release)
CURRENT BRANCH feature/f023-hybrid-fast-search
LAST DECISION  D-025: Streaming assíncrono direto do es.exe sem arquivos CSV temporários no %TEMP%
NEXT ACTION    Integração linear na main via squash & rebase
BLOCKER        Nenhum. Portão verify_no_index_service.ps1 validado com sucesso (código 0).
SIDE WORK      Próxima feature a ser criada em branch feature/<nome>
F017 WIN+E     Concluído, empacotamento AppX completo corrigido e funcionando
LAST UPDATE    2026-10-18 — AGY (Instalador v4.3.0 gerado com sucesso)
```

