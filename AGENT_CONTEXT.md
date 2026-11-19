# CONTEXTO DE DESENVOLVIMENTO

Estado atual, curto. Atualize ao mudar de fase/tarefa (METODO.md §1).

```text
PROJECT        KyraFiles / KyraFiles. Regras: MASTER_SPEC.md; continuidade: docs/agents/CONTINUITY.md
OBJECTIVE      File manager moderno com UX mais previsível. Ver MASTER_SPEC.md §2 e §44
REGRA DA MAIN  PROIBIDO alterar direto na main. AGY e Claude criam branches próprias (feature/<nome>) e integram via squash/rebase + ff-only mantendo histórico linear.
REGRA WORKFLOW DESCONTINUAÇÃO DA COLAB (09/11/2026 - Alexandre): Branch colab descontinuada e apagada local e remotamente. O projeto agora trabalha exclusivamente com a main (branches feature/<nome> saindo da main e integradas via squash/rebase + ff-only).
REGRA CICLO    RELEASE TRAINS (18/11/2026 - Alexandre, D-TRAIN-1): Lotes de 3–5 SPECs (~15–20 pts) por Train, com 1 build Release x64 (0/0), 1 bateria consolidada de validação/UIA/Gate F021/Win+E e 1 instalador por lote.
REGRA PERF     MANDATÓRIO: Em HIPÓTESE ALGUMA devolver na main com performance pior que a main anterior. Sem lentidão.
REGRA RELAT    Todo final de lote/spec exige relatório de conclusão com detalhes importantes, pontos de atenção e comparativo de performance.
FILA           ⚪ [LOTE 2 - v4.3.23] F048, F038 (10p, concluídos) · 🟢 [LOTE 3] F040, F051, F054, F055 (20p) · 🟢 [LOTE 4] F060-Onda A, F070, F071 (13p)
REGRA FOCO     MANDATÓRIO (29/10/2026): NUNCA roubar o foco da tela do usuário. Na HELOIZE-PC, testes podem rodar o tempo todo sem janela visível/foco.
REGRA VERSAO   MANDATÓRIO (21/10/2026): O número da versão DEVE ser incrementado a CADA atualização integrada na main.
MAQUINA        18/11/2026 (Alexandre): Desenvolvimento 100% centralizado na HELOIZE-PC; testes liberados o tempo todo; produção contínua sem IA ociosa.
CURRENT BRANCH feature/train-v4.3.23
LAST DECISION  18/11/2026: D-TRAIN-1 (Release Trains em lotes), D-F090-1 (Opção A conservadora), D-F060-2 (SFTP/Script no backlog final).
NEXT ACTION    /ship do Train v4.3.23 (commit-push -> merge push -> deploy) e continuidade imediata com F040.
BLOCKER        Nenhum bloqueador técnico.
PRODUCAO       2026-11-13: protocolo de controle de producao v2 adotado; fonte de verdade em docs/production/production-control.json (SPECS.md gerado dele).