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
FILA           ⚪ [CONCLUÍDO] F051 (5p, Code Search) · ⚪ [CONCLUÍDO] F055 (5p, Repo Map) · 🟡 [PROXIMO] F054 (5p, Technical Preview, preparar plan/tasks) · 🟢 [LOTE 4] F060-Onda A, F070, F071 (13p)
REGRA FOCO     MANDATÓRIO (29/10/2026): NUNCA roubar o foco da tela do usuário. Na HELOIZE-PC, testes podem rodar o tempo todo sem janela visível/foco.
REGRA VERSAO   MANDATÓRIO (21/10/2026): O número da versão DEVE ser incrementado a CADA atualização integrada na main.
MAQUINA        18/11/2026 (Alexandre): Desenvolvimento 100% centralizado na HELOIZE-PC; testes liberados o tempo todo; produção contínua sem IA ociosa.
CURRENT BRANCH feature/F055-repo-map (AGY, 21/11/2026): T1..T3 concluidas com 9 testes verdes (21 consolidados), Gate F021 OK, relatorio emitido em docs/specs/F055-repo-map/RELATORIO.md.
LAST DECISION  21/11/2026: F055 concluida com motor puro em KyraFiles.Shared/RepoMap/ e dialogo WinUI 3; pronta para commit e integracao na main.
NEXT ACTION    Comitar feature/F055-repo-map e integrar na main; verificar se F054 ou proximas specs estao prontas.
BLOCKER        Nenhum.
PRODUCAO       2026-11-13: protocolo de controle de producao v2 adotado; fonte de verdade em docs/production/production-control.json (SPECS.md gerado dele).