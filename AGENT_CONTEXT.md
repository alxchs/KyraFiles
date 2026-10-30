# CONTEXTO DE DESENVOLVIMENTO

Estado atual, curto. Atualize ao mudar de fase/tarefa (METODO.md §1).

```text
PROJECT        Files Custom (derivado do Files Community). Regras: MASTER_SPEC.md; continuidade: docs/agents/CONTINUITY.md
OBJECTIVE      File manager moderno com UX mais previsível. Ver MASTER_SPEC.md §2 e §44
CURRENT PHASE  F063 concluída / Preparação F064 (Memória Ativa: Tab & Page Unloading)
CURRENT TASK   F063 integrada na colab; Parcial 4.3.9 gerada (FilesCustomSetup-4.3.9.exe); Handoff para Claude
ACCEPTANCE     Working Set minimizado < 10 MB (3.6-4.5 MB), restauração < 46 ms, Compilação 0/0, Gate F021 verde (CONFIRMED)
REGRA DA MAIN  PROIBIDO alterar direto na main. AGY e Claude criam branches próprias (feature/<nome>) e integram via squash/rebase.
REGRA CICLO    MANDATÓRIO (29/10/2026): Cada spec, uma branch saindo da main/colab. Roteiro de testes executado. Devolução na colab antes de iniciar próxima spec.
REGRA PERF     MANDATÓRIO (24 e 29/10/2026): Em HIPÓTESE ALGUMA devolver na colab com performance pior que a colab. Sem lentidão.
REGRA COLAB    Em hipótese alguma iniciar nova spec sem colab atualizada. colab espelha a main exclusivamente por fast-forward (--ff-only).
REGRA RELAT    Todo final de spec exige relatório de conclusão com detalhes importantes, pontos de atenção e comparativo de performance.
REGRA FOCO     MANDATÓRIO (29/10/2026): NUNCA roubar o foco da tela do usuário. Usar -NoLaunch e testes não-intrusivos.
REGRA VERSAO   MANDATÓRIO (21/10/2026): O número da versão DEVE ser incrementado a CADA atualização integrada na main.
DO NOT CHANGE  Nada na main diretamente. Trabalho somente em branch de feature.
OWNER          AGY / Claude
CURRENT BRANCH colab / main
LAST DECISION  29/10/2026: F063 integrada e build parcial 4.3.9 gerado com sucesso em artifacts/Setup/. Handoff consolidado para Claude.
NEXT ACTION    Claude assume a partir da colab para conduzir F064 ou tarefas pendentes da esteira.
BLOCKER        Nenhum
LAST UPDATE    2026-10-29 21:00 - AGY
```
