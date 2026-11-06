# CONTEXTO DE DESENVOLVIMENTO

Estado atual, curto. Atualize ao mudar de fase/tarefa (METODO.md §1).

```text
PROJECT        Files Custom (derivado do Files Community). Regras: MASTER_SPEC.md; continuidade: docs/agents/CONTINUITY.md
OBJECTIVE      File manager moderno com UX mais previsível. Ver MASTER_SPEC.md §2 e §44
CURRENT PHASE  Solicitação de SDD: Correção de Fontes e Escala de Visualização de Tela
CURRENT TASK   Solicitação formal preparada para a Claude em docs/agents/handoffs/2026-11-04_1845_agy_solicitacao-claude-fontes-e-escala-visual.md contendo diagnóstico completo das fontes (string vs FontFamily em AppResourcesService, ThemeDictionaries do WinUI 3 e TextBlockStyles) e especificação necessária para Escala da Interface (UI Scale).
ACCEPTANCE     Aguardando a Claude preparar e validar os pacotes SDD (spec.md, plan.md, tasks.md) para início da execução pela AGY.
REGRA DA MAIN  PROIBIDO alterar direto na main. AGY e Claude criam branches próprias (feature/<nome>) e integram via squash/rebase.
REGRA CICLO    MANDATÓRIO (29/10/2026): Cada spec, uma branch saindo da main/colab. Roteiro de testes executado. Devolução na colab antes de iniciar próxima spec.
REGRA PERF     MANDATÓRIO (24 e 29/10/2026): Em HIPÓTESE ALGUMA devolver na colab com performance pior que a colab. Sem lentidão.
REGRA COLAB    Em hipótese alguma iniciar nova spec sem colab atualizada. colab espelha a main exclusivamente por fast-forward (--ff-only).
REGRA RELAT    Todo final de spec exige relatório de conclusão com detalhes importantes, pontos de atenção e comparativo de performance.
FILA          KyraFiles (31/10/2026): ordem de trabalho em docs/agents/FILA-KYRAFILES.md; Item 1 a 4 entregues; F086 entregue; F052 pronta; Demanda do Alexandre: Correção de Fontes e Escala de Visualização.
REGRA FOCO     MANDATÓRIO (29/10/2026): NUNCA roubar o foco da tela do usuário. Usar -NoLaunch e testes não-intrusivos.
REGRA VERSAO   MANDATÓRIO (21/10/2026): O número da versão DEVE ser incrementado a CADA atualização integrada na main.
DO NOT CHANGE  Nada na main diretamente. Trabalho somente em branch de feature.
OWNER          Claude (preparação de SDD) / AGY (execução)
MAQUINA NOVA   06/11/2026: comece por docs/agents/handoffs/2026-11-06_1900_claude_passagem-para-outra-maquina.md (estado do git, ambiente e memória do Claude a restaurar).
DUAS MAQUINAS  06/11/2026 (Alexandre): desenvolve na Heloize-PC (sem Visual Studio nem Windows SDK; build impossível lá) e compila na máquina antiga, que tem tudo instalado. Build, Verde.ps1 -Mark e push saem da máquina antiga.
CURRENT BRANCH main (06/11/2026: a feature/f088-legacy-path-residues, com 26 caminhos legados src/Files.App corrigidos em configs e scripts, foi integrada por squash num único commit sobre origin/main; build NOT TESTED). Push pendente: só a máquina antiga, depois do build verde, e a colab antes da main.
LAST DECISION  04/11/2026: Diagnóstico de fontes e proposta de escala visual transferidos para a Claude estruturar as specs conforme orientação do Alexandre.
NEXT ACTION    A Claude elabora spec.md, plan.md e tasks.md e avisa a AGY para iniciar a implementação.
BLOCKER        Nenhum
LAST UPDATE    2026-11-06 - Claude (passagem para a Heloize-PC; correção de caminhos legados)
```
