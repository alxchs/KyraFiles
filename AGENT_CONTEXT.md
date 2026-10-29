# CONTEXTO DE DESENVOLVIMENTO

Estado atual, curto. Atualize ao mudar de fase/tarefa (METODO.md §1).

```text
PROJECT        Files Custom (derivado do Files Community). Regras: MASTER_SPEC.md; continuidade: docs/agents/CONTINUITY.md
OBJECTIVE      File manager moderno com UX mais previsível. Ver MASTER_SPEC.md §2 e §44
CURRENT PHASE  F063 (Prioridade Zero - P0): Otimização de Consumo de Memória sem Degradação de Latência
CURRENT TASK   Integração concluída na main/colab (Working Set reduzido em até 98.5%, latência < 46 ms)
ACCEPTANCE     Compilação 0/0, Gate F021 verde, Working Set minimizado < 10 MB, latência < 46 ms (CONFIRMED)
REGRA DA MAIN  PROIBIDO alterar direto na main. AGY e Claude criam branches próprias (feature/<nome>) e integram via squash/rebase.
REGRA CICLO    MANDATÓRIO (29/10/2026): Cada spec, uma branch saindo da main/colab. Roteiro de testes executado. Devolução na colab antes de iniciar próxima spec.
REGRA PERF     MANDATÓRIO (24 e 29/10/2026): Em HIPÓTESE ALGUMA devolver na colab com performance pior que a colab. Sem lentidão.
REGRA COLAB    Em hipótese alguma iniciar nova spec sem colab atualizada. colab espelha a main exclusivamente por fast-forward (--ff-only).
REGRA RELAT    Todo final de spec exige relatório de conclusão com detalhes importantes, pontos de atenção e comparativo de performance.
REGRA VERSAO   MANDATÓRIO (21/10/2026): O número da versão DEVE ser incrementado a CADA atualização integrada na main.
DO NOT CHANGE  Nada na main diretamente. Trabalho somente em branch de feature.
OWNER          AGY / Claude
CURRENT BRANCH main
LAST DECISION  29/10/2026: F063 concluída com sucesso: Working Set reduzido de 246 MB para 3.6-4.4 MB em minimização com restauração instantânea (38-46 ms).
NEXT ACTION    Disponível para aprovação de push e escolha da próxima spec pelo Alexandre.
BLOCKER        Nenhum
LAST UPDATE    2026-10-29 - AGY
```
