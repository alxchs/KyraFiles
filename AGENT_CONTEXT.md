# CONTEXTO DE DESENVOLVIMENTO

Estado atual, curto. Atualize ao mudar de fase/tarefa (METODO.md §1).

```text
PROJECT        Files Custom / KyraFiles. Regras: MASTER_SPEC.md; continuidade: docs/agents/CONTINUITY.md
OBJECTIVE      File manager moderno com UX mais previsível. Ver MASTER_SPEC.md §2 e §44
CURRENT PHASE  F089: Fontes Não Aplicadas (Execução de Código Concluída)
CURRENT TASK   [PEGO] F089 · AGY · 2026-11-08 12:35 · branch feature/f089-fontes · pronto = FontFamily aplicado com segurança em AppResourcesService, herança corrigida nos 3 layouts e DataGridHeader, persistência e troca imediata · NÃO inclui = prova de tela e build de release (depende da máquina de compilação)
ACCEPTANCE     T1, T2, T3 e T5 concluídos em código; T4 (build/tela) reservado para a máquina de compilação com Windows SDK.
REGRA DA MAIN  PROIBIDO alterar direto na main. AGY e Claude criam branches próprias (feature/<nome>) e integram via squash/rebase.
REGRA CICLO    MANDATÓRIO (29/10/2026): Cada spec, uma branch saindo da main/colab. Roteiro de testes executado. Devolução na colab antes de iniciar próxima spec.
REGRA PERF     MANDATÓRIO (24 e 29/10/2026): Em HIPÓTESE ALGUMA devolver na colab com performance pior que a colab. Sem lentidão.
REGRA COLAB    Em hipótese alguma iniciar nova spec sem colab atualizada. colab espelha a main exclusivamente por fast-forward (--ff-only).
REGRA RELAT    Todo final de spec exige relatório de conclusão com detalhes importantes, pontos de atenção e comparativo de performance.
FILA           [LIVRE] F090 · depende de F089 integrada na colab · pronto = Escala de interface proporcional sem distorção
REGRA FOCO     MANDATÓRIO (29/10/2026): NUNCA roubar o foco da tela do usuário. Usar -NoLaunch e testes não-intrusivos.
REGRA VERSAO   MANDATÓRIO (21/10/2026): O número da versão DEVE ser incrementado a CADA atualização integrada na main.
DO NOT CHANGE  Nada na main diretamente. Trabalho somente em branch de feature.
OWNER          AGY (execução)
MAQUINA NOVA   06/11/2026: Heloize-PC opera em modo desenvolvimento (edição de código e testes lógicos). Compilação oficial e pacote na máquina com Windows SDK.
DUAS MAQUINAS  06/11/2026 (Alexandre): desenvolve na Heloize-PC (sem Visual Studio nem Windows SDK; build impossível lá) e compila na máquina antiga, que tem tudo instalado. Build, Verde.ps1 -Mark e push saem da máquina antiga.
CURRENT BRANCH feature/f089-fontes (baseada em colab / 2d062627e)
LAST DECISION  08/11/2026: F089 implementada em código com gravação de FontFamily e propagação nos ThemeDictionaries e recursos dos 3 layouts e DataGridHeader.
NEXT ACTION    Validar compilação e teste visual na máquina antiga para fechamento do portão T4 e integração na colab.
BLOCKER        Nenhum
LAST UPDATE    2026-11-08 - AGY (implementação de código da F089 na feature/f089-fontes)
```
