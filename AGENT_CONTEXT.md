# CONTEXTO DE DESENVOLVIMENTO

Estado atual, curto. Atualize ao mudar de fase/tarefa (METODO.md §1).

```text
PROJECT        Files Custom (derivado do Files Community). Regras: MASTER_SPEC.md; continuidade: docs/agents/CONTINUITY.md
OBJECTIVE      File manager moderno com UX mais previsível. Ver MASTER_SPEC.md §2 e §44
CURRENT PHASE  F063 (Prioridade Zero - P0): Otimização de Consumo de Memória sem Degradação de Latência
CURRENT TASK   Alinhamento de arquitetura com Claude (Iniciativas de memória P0 vs fluidez zero-jank)
ACCEPTANCE     Compilação 0/0, Gate F021 verde, Working Set em repouso < 80 MB, latência de reativação < 50 ms
REGRA DA MAIN  PROIBIDO alterar direto na main. AGY e Claude criam branches próprias (feature/<nome>) e integram via squash/rebase.
REGRA VERSAO   MANDATÓRIO (21/10/2026): O número da versão DEVE ser incrementado a CADA atualização integrada na main.
REGRA PERF     MANDATÓRIO (24/10/2026): NENHUMA alteração deve ser aceita se onerar a performance; analisar e relatar ganhos.
DO NOT CHANGE  Nada na main diretamente. Trabalho somente em branch de feature.
REGRA COLAB    A branch colab e custom/main espelham a main exclusivamente por fast-forward (--ff-only).
OWNER          AGY / Claude
CURRENT BRANCH feature/release-4.3.8-integrated
LAST DECISION  29/10/2026: Alexandre define Prioridade Zero (P0) para redução de consumo de memória com equilíbrio inegociável de fluidez (zero lentidão).
NEXT ACTION    Validação da proposta com Claude e abertura de spec F063.
BLOCKER        Nenhum
LAST UPDATE    2026-10-29 - AGY
```
