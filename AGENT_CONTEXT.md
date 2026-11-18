# CONTEXTO DE DESENVOLVIMENTO

Estado atual, curto. Atualize ao mudar de fase/tarefa (METODO.md §1).

```text
PROJECT        KyraFiles / KyraFiles. Regras: MASTER_SPEC.md; continuidade: docs/agents/CONTINUITY.md
OBJECTIVE      File manager moderno com UX mais previsível. Ver MASTER_SPEC.md §2 e §44
CURRENT PHASE  Release v4.3.21.0 (Hotfix Crash WindowEx_Activated + Busca por Digitação + Publish-Release.ps1)
CURRENT TASK   ⚪ [FEITO] Crash 0xc0000005 em WindowEx_Activated corrigido, busca por digitação (type-ahead) restaurada e Publish-Release.ps1 corrigido
ACCEPTANCE     ACs e testes unitários aprovados (110/110), Gate F021 verificado
REGRA DA MAIN  PROIBIDO alterar direto na main. AGY e Claude criam branches próprias (feature/<nome>) e integram via squash/rebase + ff-only mantendo histórico linear.
REGRA WORKFLOW DESCONTINUAÇÃO DA COLAB (09/11/2026 - Alexandre): Branch colab descontinuada e apagada local e remotamente. O projeto agora trabalha exclusivamente com a main (branches feature/<nome> saindo da main e integradas via squash/rebase + ff-only).
REGRA CICLO    MANDATÓRIO: Cada spec, uma branch saindo da main atualizada. Roteiro de testes executado. Devolução na main antes de iniciar próxima spec.
REGRA PERF     MANDATÓRIO: Em HIPÓTESE ALGUMA devolver na main com performance pior que a main anterior. Sem lentidão.
REGRA RELAT    Todo final de spec exige relatório de conclusão com detalhes importantes, pontos de atenção e comparativo de performance.
FILA           🟢 [LIVRE] F042, F038, F040, F043, F045, F048 · ⚪ [EM VALIDACAO] F060, F062, F063, F090
REGRA FOCO     MANDATÓRIO (29/10/2026): NUNCA roubar o foco da tela do usuário. Usar -NoLaunch e testes não-intrusivos.
REGRA VERSAO   MANDATÓRIO (21/10/2026): O número da versão DEVE ser incrementado a CADA atualização integrada na main.
DO NOT CHANGE  Nada na main diretamente. Trabalho somente em branch de feature.
OWNER          AGY (execução)
MAQUINA NOVA   06/11/2026: Heloize-PC opera em modo desenvolvimento (edição de código e testes lógicos). Compilação oficial e pacote na máquina com Windows SDK.
DUAS MAQUINAS  06/11/2026 (Alexandre): desenvolve na Heloize-PC (sem Visual Studio nem Windows SDK; build impossível lá) e compila na máquina antiga, que tem tudo instalado. Build, Verde.ps1 -Mark e push saem da máquina antiga.
CURRENT BRANCH main
LAST DECISION  18/11/2026: v4.3.21 publicada com correção do crash WindowEx_Activated, type-ahead com suporte a acentos e Publish-Release.ps1 corrigido.
NEXT ACTION    Validar v4.3.21 nas duas máquinas e prosseguir com validação visual dos itens em in_validation.
BLOCKER        Nenhum bloqueador técnico.
LAST UPDATE    2026-11-18 06:30 - AGY (Release v4.3.21.0)
PRODUCAO       2026-11-13: protocolo de controle de producao v2 adotado; fonte de verdade em docs/production/production-control.json (SPECS.md gerado dele).
```
