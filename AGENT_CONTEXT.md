# CONTEXTO DE DESENVOLVIMENTO

Estado atual, curto. Atualize ao mudar de fase/tarefa (METODO.md §1).

```text
PROJECT        Files Custom (derivado do Files Community). Regras: MASTER_SPEC.md; continuidade: docs/agents/CONTINUITY.md
OBJECTIVE      File manager moderno com UX mais previsível. Ver MASTER_SPEC.md §2 e §44
CURRENT PHASE  F068 (Documentação Local Offline com Fallback Web) e F067 (Sponsors & PowerShell Moderno) concluídas
CURRENT TASK   F068 e F067 validados, compilados em Release e implantados em %LOCALAPPDATA%\Programs\FilesCustom
ACCEPTANCE     166 testes unitários passando (0 falhas); compilação Release 0 erros; F021 verificado com sucesso; documentação offline integrada e verificada
REGRA DA MAIN  PROIBIDO alterar direto na main. AGY e Claude criam branches próprias (feature/<nome>) e integram via squash/rebase.
REGRA CICLO    MANDATÓRIO (29/10/2026): Cada spec, uma branch saindo da main/colab. Roteiro de testes executado. Devolução na colab antes de iniciar próxima spec.
REGRA PERF     MANDATÓRIO (24 e 29/10/2026): Em HIPÓTESE ALGUMA devolver na colab com performance pior que a colab. Sem lentidão.
REGRA COLAB    Em hipótese alguma iniciar nova spec sem colab atualizada. colab espelha a main exclusivamente por fast-forward (--ff-only).
REGRA RELAT    Todo final de spec exige relatório de conclusão com detalhes importantes, pontos de atenção e comparativo de performance.
FILA          KyraFiles (31/10/2026): ordem de trabalho em docs/agents/FILA-KYRAFILES.md, itens 0 a 10 em série; nada começa antes do portão P0; logo do KyraDB bloqueada (arte não encontrada)
REGRA FOCO     MANDATÓRIO (29/10/2026): NUNCA roubar o foco da tela do usuário. Usar -NoLaunch e testes não-intrusivos.
REGRA VERSAO   MANDATÓRIO (21/10/2026): O número da versão DEVE ser incrementado a CADA atualização integrada na main.
DO NOT CHANGE  Nada na main diretamente. Trabalho somente em branch de feature.
OWNER          AGY
CURRENT BRANCH colab
LAST DECISION  31/10/2026: F068 (Docs locais offline em HTML nativo com prioridade total e fallback web no DocumentationHelper)
NEXT ACTION    Disponível para uso imediato pelo Alexandre
BLOCKER        Nenhum
LAST UPDATE    2026-10-31 11:18 - AGY (F068 e F067 entregues na colab e instalados em %LOCALAPPDATA%\Programs\FilesCustom)
```
