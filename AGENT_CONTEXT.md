# CONTEXTO DE DESENVOLVIMENTO

Estado atual, curto. Atualize ao mudar de fase/tarefa (METODO.md §1).

```text
PROJECT        Files Custom (derivado do Files Community). Regras: MASTER_SPEC.md; continuidade: docs/agents/CONTINUITY.md
OBJECTIVE      File manager moderno com UX mais previsível. Ver MASTER_SPEC.md §2 e §44
CURRENT PHASE  F063 (Prioridade Zero - P0): Otimização de Consumo de Memória sem Degradação de Latência
CURRENT TASK   Alinhamento de arquitetura com Claude (Iniciativas de memória P0 vs fluidez zero-jank)
ACCEPTANCE     Compilação 0/0, Gate F021 verde, Working Set em repouso < 80 MB, latência de reativação < 50 ms
REGRA DA MAIN  PROIBIDO alterar direto na main. AGY e Claude criam branches próprias (feature/<nome>) e integram via squash/rebase.
REGRA CICLO    MANDATÓRIO (29/10/2026): Cada spec, uma branch saindo da main/colab. Roteiro de testes executado. Devolução na colab antes de iniciar próxima spec.
REGRA PERF     MANDATÓRIO (24 e 29/10/2026): Em HIPÓTESE ALGUMA devolver na colab com performance pior que a colab. Sem lentidão.
REGRA COLAB    Em hipótese alguma iniciar nova spec sem colab atualizada. colab espelha a main exclusivamente por fast-forward (--ff-only).
REGRA RELAT    Todo final de spec exige relatório de conclusão com detalhes importantes, pontos de atenção e comparativo de performance.
REGRA VERSAO   MANDATÓRIO (21/10/2026): O número da versão DEVE ser incrementado a CADA atualização integrada na main.
DO NOT CHANGE  Nada na main diretamente. Trabalho somente em branch de feature.
OWNER          AGY / Claude
CURRENT BRANCH feature/f063-memory-optimization-p0
LAST DECISION  29/10/2026: Alexandre define Prioridade Zero (P0) para memória e fixa as 5 regras soberanas de specs e colab.
NEXT ACTION    Validação da proposta com Claude, abertura de spec F063 e roteiro de testes.
BLOCKER        Nenhum
LAST UPDATE    2026-10-29 - AGY
```
