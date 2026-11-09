# CONTEXTO DE DESENVOLVIMENTO

Estado atual, curto. Atualize ao mudar de fase/tarefa (METODO.md §1).

```text
PROJECT        Files Custom / KyraFiles. Regras: MASTER_SPEC.md; continuidade: docs/agents/CONTINUITY.md
OBJECTIVE      File manager moderno com UX mais previsível. Ver MASTER_SPEC.md §2 e §44
CURRENT PHASE  F090: Escala da Interface (Execução - Código Concluído)
CURRENT TASK   [FEITO] F090 Pacotes 1 e 2 · T1, T2 e T3 concluídos em código · pronto = Modelo, persistência, interface em Configurações e serviço IUiScaleService com layout compensado na MainWindow
ACCEPTANCE     T1, T2 e T3 concluídos em código; T4 e T5 (build oficial e comprovação visual) reservados para máquina de compilação
REGRA DA MAIN  PROIBIDO alterar direto na main. AGY e Claude criam branches próprias (feature/<nome>) e integram via squash/rebase + ff-only mantendo histórico linear.
REGRA WORKFLOW DESCONTINUAÇÃO DA COLAB (09/11/2026 - Alexandre): Branch colab descontinuada e apagada local e remotamente. O projeto agora trabalha exclusivamente com a main (branches feature/<nome> saindo da main e integradas via squash/rebase + ff-only).
REGRA CICLO    MANDATÓRIO: Cada spec, uma branch saindo da main atualizada. Roteiro de testes executado. Devolução na main antes de iniciar próxima spec.
REGRA PERF     MANDATÓRIO: Em HIPÓTESE ALGUMA devolver na main com performance pior que a main anterior. Sem lentidão.
REGRA RELAT    Todo final de spec exige relatório de conclusão com detalhes importantes, pontos de atenção e comparativo de performance.
FILA           [LIVRE] F090 Pacote 3 · T5 (Validação visual na máquina de compilação e fechamento da spec)
REGRA FOCO     MANDATÓRIO (29/10/2026): NUNCA roubar o foco da tela do usuário. Usar -NoLaunch e testes não-intrusivos.
REGRA VERSAO   MANDATÓRIO (21/10/2026): O número da versão DEVE ser incrementado a CADA atualização integrada na main.
DO NOT CHANGE  Nada na main diretamente. Trabalho somente em branch de feature.
OWNER          AGY (execução)
MAQUINA NOVA   06/11/2026: Heloize-PC opera em modo desenvolvimento (edição de código e testes lógicos). Compilação oficial e pacote na máquina com Windows SDK.
DUAS MAQUINAS  06/11/2026 (Alexandre): desenvolve na Heloize-PC (sem Visual Studio nem Windows SDK; build impossível lá) e compila na máquina antiga, que tem tudo instalado. Build, Verde.ps1 -Mark e push saem da máquina antiga.
CURRENT BRANCH feature/f090-escala (baseada na main / 220ff3950)
LAST DECISION  09/11/2026: T1, T2 e T3 da F090 implementados em código (IUiScaleService, MainWindow, DragZoneHelper, Settings).
NEXT ACTION    Comitar Pacote 2 na feature/f090-escala e gerar relatório/handoff.
BLOCKER        Nenhum
LAST UPDATE    2026-11-09 - AGY (conclusão dos Pacotes 1 e 2 da F090 em código)
```
