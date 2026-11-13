# CONTEXTO DE DESENVOLVIMENTO

Estado atual, curto. Atualize ao mudar de fase/tarefa (METODO.md §1).

```text
PROJECT        Files Custom / KyraFiles. Regras: MASTER_SPEC.md; continuidade: docs/agents/CONTINUITY.md
OBJECTIVE      File manager moderno com UX mais previsível. Ver MASTER_SPEC.md §2 e §44
CURRENT PHASE  F092: Paridade da Barra Lateral com o Explorador Nativo (INTEGRADA NA MAIN / v4.3.13)
CURRENT TASK   [FEITO] F092 Concluída e Integrada na Main (v4.3.13) · T0 a T6 implementadas, 39 testes lógicos verdes, versão 4.3.13 incrementada, relatório formal docs/specs/F092-native-sidebar-parity/RELATORIO.md gerado e aprovado
ACCEPTANCE     AC-1 a AC-5 definidos em docs/specs/F092-native-sidebar-parity/spec.md
REGRA DA MAIN  PROIBIDO alterar direto na main. AGY e Claude criam branches próprias (feature/<nome>) e integram via squash/rebase + ff-only mantendo histórico linear.
REGRA WORKFLOW DESCONTINUAÇÃO DA COLAB (09/11/2026 - Alexandre): Branch colab descontinuada e apagada local e remotamente. O projeto agora trabalha exclusivamente com a main (branches feature/<nome> saindo da main e integradas via squash/rebase + ff-only).
REGRA CICLO    MANDATÓRIO: Cada spec, uma branch saindo da main atualizada. Roteiro de testes executado. Devolução na main antes de iniciar próxima spec.
REGRA PERF     MANDATÓRIO: Em HIPÓTESE ALGUMA devolver na main com performance pior que a main anterior. Sem lentidão.
REGRA RELAT    Todo final de spec exige relatório de conclusão com detalhes importantes, pontos de atenção e comparativo de performance.
FILA           🟢 [LIVRE] F036 (fila de cópia com limite por destino, pronta para início em branch própria feature/f036-copy-queue)
REGRA FOCO     MANDATÓRIO (29/10/2026): NUNCA roubar o foco da tela do usuário. Usar -NoLaunch e testes não-intrusivos.
REGRA VERSAO   MANDATÓRIO (21/10/2026): O número da versão DEVE ser incrementado a CADA atualização integrada na main.
DO NOT CHANGE  Nada na main diretamente. Trabalho somente em branch de feature.
OWNER          AGY (execução)
MAQUINA NOVA   06/11/2026: Heloize-PC opera em modo desenvolvimento (edição de código e testes lógicos). Compilação oficial e pacote na máquina com Windows SDK.
DUAS MAQUINAS  06/11/2026 (Alexandre): desenvolve na Heloize-PC (sem Visual Studio nem Windows SDK; build impossível lá) e compila na máquina antiga, que tem tudo instalado. Build, Verde.ps1 -Mark e push saem da máquina antiga.
CURRENT BRANCH main
LAST DECISION  12/11/2026: F092 integrada na main via squash/rebase + ff-only com incremento para v4.3.13. F036 pronta para início.
NEXT ACTION    Iniciar spec F036 na branch feature/f036-copy-queue pela tarefa T2 (fila com limite por destino e medições).
BLOCKER        Nenhum bloqueador técnico.
LAST UPDATE    2026-11-12 22:25 - AGY (F092 integrada na main v4.3.13)
```
