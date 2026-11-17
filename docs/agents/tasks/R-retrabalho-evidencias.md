# R-retrabalho-evidencias — provas reais de F030, F032, F033, F034, F029 e F023

Status: APROVADO (Claude, 24/10/2026). **Só começa depois de o Claude revisar a S5 da ordem rígida** (`docs/agents/ORDEM-RIGIDA-AGY.md`) e escrever "LIBERADO". Branch: `feature/r-retrabalho-evidencias` (a partir da main atual).

## Por quê
A v4.3.6 foi publicada com evidência que não prova os AC (capturas da janela errada, painel não aberto, aba nunca fixada, nenhum número de desempenho). O `STATUS.md` já foi reaberto (INFERRED). Este brief é o único caminho para voltar a CONFIRMED.

## Regras (valem em todas as tarefas)
- Toda entrada global passa por `tools/perf/Send-GuardedInput.ps1` (revisado). Nenhum `SendKeys`/`SendInput`/`keybd_event`/`mouse_event` fora dele.
- Instância própria do KyraFiles, iniciada pelo script, com PID guardado. Nunca anexar ao app do Alexandre.
- Cada AC vira uma linha de `resultado.csv` (`AC, esperado, observado, PASS/FAIL, comando`); o relatório `.md` é **gerado** do CSV; captura da **janela inteira** é anexo, nunca a prova.
- Cada feature tem controle negativo (o teste reprova onde deve).
- Registrar no começo de cada rodada: caminho e versão do `KyraFiles.exe`, commit, `KyraFiles.InstallLocation`.
- Número de desempenho só com o comando que o mediu, 3 rodadas, mesmos scripts antes e depois (METODO §11).
- Se um AC reprovar: **não** corrigir de passagem. Registrar `FAIL`, abrir `DISCOVERY` e avisar o Claude (vira bug P0/P1).
- Sem push, tag, release ou merge. Uma tarefa por commit.

## Tarefas (nesta ordem; pare ao fim de cada par e avise o Claude)

**R1 — F034 (lista sem caixa).** AC-1 a AC-6 da spec. Detalhes, Ícones e Cartões sem caixa no hover e no cabeçalho (assertar que não existe nenhum `CheckBox` na árvore de UI Automation da lista); clique no ícone seleciona só a linha, Ctrl+clique e Shift+clique funcionam; ligar a opção em Configurações > Pastas sem reiniciar faz as caixas aparecerem e marcar 2 seleciona 2; desligar as faz sumir; perfil limpo abre com a opção desligada. Controle negativo: com a opção ligada, o mesmo teste **tem** de achar `CheckBox`. Atenção do Claude: o código reage à mudança da opção por `RefreshItems`; AC-4 e AC-5 ainda **nunca foram observados**.

**R2 — F032 (fixar aba).** AC-1 a AC-5: 4 abas, fixar a 3ª (vai para a 1ª, com alfinete, sem botão de fechar); "Fechar outras abas"; Ctrl+W na fixada; fechar e reabrir o processo e conferir a aba fixada em 1º; desafixar devolve o botão de fechar. Assertar pela UIA (nome, posição, botão de fechar presente ou ausente). Controle negativo: uma aba **não** fixada fecha com Ctrl+W.

**R3 — F033 (copiar/mover para).** AC-1 a AC-6 em pasta de teste em `%TEMP%\agy\<GUID>\...` com 3 arquivos: submenu só com "Escolher pasta…" sem histórico; Enter no diálogo não copia; "Sim" copia e a pasta vira o 1º do submenu; 13 pastas mostram 12; "Mover para" + `Ctrl+Z` devolve; apagar a pasta a remove do submenu; reabrir o app mantém a lista. Assertar por disco (`Test-Path`, contagem) e UIA. Controle negativo: Enter **não** pode copiar.

**R4 — F030 (preview na tecla segurada).** AC-1 a AC-3: painel aberto (assertar que o painel existe), Down segurado: ≥3 quadros durante a tecla com painel vazio e 1 s depois com o item final; toque único atualiza; linhas alcançadas em 5 s com painel aberto vs fechado, 3 rodadas, CSV. Controle negativo: com a correção desligada (build anterior ou opção), o painel **não** fica vazio.

**R5 — F029 (setas como no Explorer): medir, não afirmar.** O código foi fixado (throttle de 1 quadro + `TryMoveRow`), mas o relatório não tem nenhum número. Medir, com o mesmo script e 3 rodadas: linhas alcançadas em 5 s segurando Down nas mesmas 1.600 pastas, no KyraFiles atual **e** no Explorer do Windows (`CabinetWClass`) e no build da v4.3.5. Entregar `docs/agents/evidence/F029/medicao.csv` e a tabela. Se o KyraFiles ficar abaixo do Explorer, é pendência de performance P1: registrar e avisar o Claude.

**R6 — F023 (busca) e a correção do Omnibar.** (a) Reproduzir o crash **antes**: no build da v4.3.5, abrir o Omnibar, digitar e apertar Esc N vezes (script guardado, instância própria) e registrar se há `Access Violation` no log de eventos; (b) repetir no build atual e comparar. Sem reprodução antes, a correção fica INFERRED e é assim que será registrada. (c) Refazer a bateria de 15 buscas com o script guardado e medir handles e memória do **processo de teste** (comando `Get-Process`), 3 rodadas.

**R7 — Fechamento.** Relatório por AC gerado dos CSV; `STATUS.md` volta a CONFIRMED **somente** onde o CSV tem PASS e controle negativo; handoff por AC; `AGENT_CONTEXT.md`. No fim: `KyraFiles` de volta para o instalado, `bin` e GUID temporário apagados (uma cópia só do app). **Pare.** O Claude revisa e decide o que entra na próxima versão.

## O que o Claude confere
1. Cada AC tem linha no CSV e controle negativo; o relatório foi gerado do CSV.
2. A captura mostra a janela do KyraFiles inteira e o estado pedido.
3. O número de desempenho tem comando, 3 rodadas e a comparação com o Explorer.
4. Nenhuma entrada global fora do helper; nenhum processo alheio anexado.
5. Falha registrada como falha, sem correção escondida.
