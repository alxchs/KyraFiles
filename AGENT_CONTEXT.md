# CONTEXTO DE DESENVOLVIMENTO

Estado atual, curto. Atualize ao mudar de fase/tarefa (METODO.md §1).

```text
PROJECT        Files Custom (derivado do Files Community). Regras: MASTER_SPEC.md; continuidade: docs/agents/CONTINUITY.md
OBJECTIVE      File manager moderno com UX mais previsível. Ver MASTER_SPEC.md §2 e §44
CURRENT PHASE  F061 / F062 / Correções 4.3.8
CURRENT TASK   F062: Memória do ecossistema no Sobre; Fix: Tags e preservação de configurações no Setup
ACCEPTANCE     Compilação 0/0, Testes unitários 6/6 verdes, Gate F021 verificado
REGRA DA MAIN  PROIBIDO alterar direto na main. AGY e Claude criam branches próprias (feature/<nome>) e integram via squash/rebase.
REGRA VERSAO   MANDATÓRIO (21/10/2026): O número da versão DEVE ser incrementado a CADA atualização integrada na main.
REGRA PERF     MANDATÓRIO (24/10/2026): NENHUMA alteração deve ser aceita se onerar a performance; analisar e relatar ganhos.
DO NOT CHANGE  Nada na main diretamente. Trabalho somente em branch de feature.
REGRA COLAB    A branch colab e custom/main espelham a main exclusivamente por fast-forward (--ff-only).
OWNER          AGY / Claude
CURRENT BRANCH feature/f062-ecosystem-memory-about
LAST DECISION  28/10/2026: Autorizações pelo Alexandre (ADR-007 SSH.NET, ADR-008 Jint, interruptor Hex, segurança FTPS, F058 aprovada, F062 memória ecossistema).
NEXT ACTION    Squash rebase para main e colab; push autorizado (sem publicação de release oficial ainda).
BLOCKER        Nenhum
LAST UPDATE    2026-10-28 - AGY
```
