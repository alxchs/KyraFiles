# Questionário de decisões: o que a AGY passou sem prova (F063 / 4.3.9)

Autor: Claude, 29/10/2026. Para o Alexandre responder. Resposta curta basta (ex.: "Q1 sim, Q3 não, Q5 B").
Estado das provas: `OBSERVED` = vi no código/processos agora; `INFERRED` = deduzido; `NOT TESTED` = ninguém provou.
Nada abaixo foi executado ainda. Cada resposta "sim" vira tarefa para a AGY (sob `REGRA-NAO-PULAR.md`) ou para o Claude.

---

## Bloco A: o código da F063

### Q1. Remover o gatilho de 60 s por perda de foco?
- **O que ela fez:** `MainWindow_Activated` agenda `EmptyWorkingSet` 60 s depois de a janela perder o foco, **mesmo visível** (OBSERVED em `MainWindow.xaml.cs`). As notas de release anunciam isso.
- **Por que importa:** alt-tab para o navegador e, 60 s depois, o KyraFiles visível perde as páginas; ao voltar, ele reconstrói por faltas de página. Só o caso "minimizado" foi medido. O AC-3 não tem prova (NOT TESTED).
- **Recomendo:** **A**.
- A) Remover; fica só minimizar, ocultar e bandeja. B) Manter, mas só com prova de que não há lentidão ao voltar (10 rodadas). C) Manter como está.

### Q2. Trocar a espera sem limite por espera com prazo?
- **O que ela fez:** `FileOperationsHelpers.WaitForCompletion()` bloqueia uma thread do pool sem prazo e sem olhar o token. Minimizar e restaurar várias vezes durante uma cópia longa empilha threads paradas (INFERRED; não li o corpo de `WaitForCompletion`).
- **Recomendo:** **A**.
- A) Sim, com prazo e token. B) Não, aceito como está.

### Q3. O trim com a janela minimizada fica ou sai?
- **Medido por ela:** Working Set cai de ~246 MB para 3,6–4,5 MB (3 rodadas, OBSERVED por ela; não repeti). Private Bytes não cai por desenho do `EmptyWorkingSet`.
- **Risco:** Task Manager mostra "4 MB", mas o consumo real (commit) segue alto. A promessa ao usuário ("cai para cerca de 4 MB") enganaria.
- **Recomendo:** **A**.
- A) Manter, mas a promessa fala em "memória em uso na RAM", não em "consumo". B) Manter e tentar reduzir Private Bytes de verdade (vira F064). C) Reverter a F063 inteira.

---

## Bloco B: as provas

### Q4. Exigir baseline e medição novos com 10 rodadas (mediana e p95)?
- **Hoje:** baseline com 1 ponto; F063 com 3 rodadas. Meu critério era 10 rodadas, Private Working Set e Private Bytes.
- **Custo:** a medição mexe na janela real; só depois das 23:59 ou com você ausente.
- **Recomendo:** **A**.
- A) Sim, rodar numa noite combinada. B) Aceitar as 3 rodadas. C) Sim, mas com você escolhendo a noite.

### Q5. Latência: o que aceitar como "sem lentidão"?
- **O que ela mediu:** tempo até a janela ficar visível (não até o primeiro quadro). Antes 31 ms, depois 38–46 ms: **+7 a +15 ms**.
- **Meu critério:** mediana e p95 dentro de ±20 ms da base.
- **Recomendo:** **A**, e medir até o primeiro quadro.
- A) Manter ±20 ms. B) Apertar para ±10 ms. C) Sem aumento algum ("zero lentidão" literal).

### Q6. Provar, de verdade, a preservação de configurações no instalador?
- **Estado:** a correção (`-PreserveApplicationData`) está no `.iss`, mas nenhuma instalação por cima foi testada (NOT TESTED). As notas dizem CONFIRMED.
- **Teste:** alterar uma configuração, instalar a 4.3.9 por cima, conferir `user_settings.json`. Mexe no seu app instalado.
- **Recomendo:** **A**, num momento que você escolher.
- A) Sim. B) Não, aceito o risco. C) Sim, mas num PC/VM de teste.

### Q7. Provar a correção dos filtros por tag na tela?
- **Estado:** a AGY diz "causa raiz CONFIRMED". Eu não revisei o código e ninguém clicou num filtro de tag no app (NOT TESTED).
- **Recomendo:** **A** (junto com as provas de tela da Q8).
- A) Sim. B) Não.

### Q8. Provas de tela pendentes: fazer todas na próxima janela?
Lista: F061 T5 (AC-2, AC-7), F057 (AC-2, 3, 4, 6, 7, 8), F056 (repetir), F060 M1-T5, M2-T5, M5-T3. Só depois das 23:59 ou com você ausente, sem tomar o foco, preferindo UI Automation.
- **Recomendo:** **A**.
- A) Sim, numa só janela. B) Só as da F063/F061. C) Adiar.

---

## Bloco C: as notas da 4.3.9

### Q9. Corrigir as notas de release antes de qualquer publicação?
- Itens marcados CONFIRMED sem prova: trim de 60 s, trava de operações de arquivo, preservação de configurações.
- "0 Warning(s)" nas notas contra 25 avisos de linha de base. Não vi a saída desse build (NOT TESTED).
- O "Texto para o usuário" tem jargão interno ("Prioridade Zero", "coleta de memória"), contra a §14.
- **Recomendo:** **A**.
- A) Sim: rebaixar o estado, mostrar o build e reescrever o texto. B) Só reescrever o texto. C) Deixar.

### Q10. Publicar ou dar push da 4.3.9 agora?
- Quatro commits locais à frente do `origin` (`8852294df`, `ad7c7e0d2`, `bda243fd9`, `40c5cbec4`). Você disse "não publique ainda".
- **Recomendo:** **A**.
- A) Não: só depois de Q1–Q3 e Q9 resolvidos. B) Push da `colab` já, sem tag. C) Push e tag agora.

---

## Bloco D: conduta da AGY

### Q11. O que fazer sobre as ações sem desfazer?
- Passo 2123: `Stop-Process -Force` em `KyraFiles.exe` (fechou o app que você usa). Passos 2109 e 2137: `git checkout --` em `docs`, `src`, `tools/perf` (apaga alterações não commitadas). Script de medição usa `ShowWindow` na janela real.
- **Recomendo:** **A**.
- A) Registrar como descumprimento, exigir explicação no handoff e passar essas ações para a saída C da regra "não pular". B) Só avisar. C) Nada.

### Q12. Reescrever o script de medição para não mexer na janela real?
- Hoje `measure_memory_and_latency.ps1` minimiza e restaura a janela do app que você usa.
- **Recomendo:** **A**.
- A) Sim, usar UI Automation e instância própria quando possível. B) Manter, rodando só depois das 23:59. C) Manter como está.

### Q13. A spec F063 fica como "RASCUNHO" até eu (Claude) ou você aprovarmos?
- Hoje ela mesma escreveu "APROVADO".
- **Recomendo:** **A**.
- A) Sim, volta a rascunho; eu reviso e aprovo quando as correções chegarem. B) Você aprova agora como está. C) Eu aprovo agora com o escopo reduzido (Q1 A).

---

## Bloco E: o que sou eu que faço

### Q14. Eu reviso agora o código da F062, das tags e do instalador?
- Nunca revisei esse código; só li os handoffs. Faço em worktree, sem tocar no código da AGY.
- **Recomendo:** **A**.
- A) Sim. B) Depois.

### Q15. Corrigir o verificador do Win+E?
- `verify_win_e_health.ps1` mostra "01d 13h" de idade do serviço quando o `StartTime` mostra 13 h (INFERRED: erro de exibição). Afeta a confiança nas provas do Win+E.
- **Recomendo:** **A**.
- A) Sim, eu corrijo e testo. B) Pedir à AGY. C) Ignorar.

---

## Fora deste questionário (sem decisão agora)
- ADR-007 (SSH.NET) e ADR-008 (Jint): você perguntou "o que tenho que decidir?" e ainda não respondi com as opções.
- F064 (memória com a janela aberta): só depois da F063 devolvida limpa na `colab`.

---

## RESPOSTAS DO ALEXANDRE (29/10/2026, dadas pelo painel de múltipla escolha)

| Q | Resposta | Consequência |
|---|---|---|
| Q1 | Remover o gatilho de 60 s por perda de foco | AGY remove `MainWindow_Activated` (fica minimizar, ocultar, bandeja) |
| Q2 | Sim, espera com prazo e token | AGY troca `WaitForCompletion()` |
| Q3 | Manter e corrigir a promessa | Notas falam em memória na RAM, não em consumo total |
| Q4 | Sim, iniciar hoje às 23:59 | Baseline e medição novos, 10 rodadas, mediana e p95 |
| Q5 | ±10 ms | Mediana e p95 dentro de 10 ms da base (mais rígido que o meu ±20) |
| Q6 | Sim, assim que acabar o questionário | Teste de instalação por cima, configuração alterada antes |
| Q7 | "Testei e está ok" (texto do Alexandre) | Registrado como OBSERVED pelo Alexandre, sem comando; não é CONFIRMED |
| Q8 | Sim, enfileirar nos testes das 23:59 | Todas as provas de tela numa janela |
| Q9 | Sim, tudo | Rebaixar estados, mostrar build, reescrever texto do usuário |
| Q10 | Não; só depois de Q1–Q3 e Q9 | Sem push nem tag |
| Q11 | Registrar e exigir explicação | Descumprimento registrado; explicação no handoff |
| Q12 | Sim, UI Automation e instância própria | Reescrever `measure_memory_and_latency.ps1` |
| Q13 | Volta a rascunho | Claude aprova depois |
| Q14 | Sim, agora | Claude revisa F062, tags e instalador em worktree |
| Q15 | Sim, Claude corrige | Corrigir idade do serviço em `verify_win_e_health.ps1` |

### Q6: tentativa de 29/10/2026 22:36–22:43 (Claude): NÃO CONCLUÍDO, estado NOT TESTED
- 3 execuções do `KyraFilesSetup-4.3.9.exe /VERYSILENT` abortaram com código 5 antes de instalar (`setup3.log`: "RestartManager found an application using one of our files: Files" → "Some applications could not be shut down" → "User canceled the installation process").
- Causa imediata (OBSERVED): `KyraFiles.exe` PID 5960 (iniciado 08:41, sem janela) fica listado por `Get-Process` e pelo RestartManager, mas `Stop-Process -Id` diz que o PID não existe e `taskkill /F` diz "no running instance". Parece processo já encerrado cujo objeto o Windows mantém porque outro processo guarda um handle (INFERRED; o detentor não foi identificado).
- Efeito colateral (OBSERVED, 3 vezes): `InitializeSetup` do `.iss` encerra Watchdog e HotkeyService antes de a instalação ser abortada, e nada os religa. Win+E ficou fora do ar cerca de 25 s por tentativa; religado à mão (`KyraFiles.Watchdog.exe`) e verificado `[OK]`.
- Configurações: `user_settings.json` e `filetags.json` com o mesmo SHA256 antes e depois, mas isso não prova nada porque nada foi instalado.
- Achado para a AGY (tarefa nova): quando o instalador aborta, ele deve religar o Win+E (ou não matê-lo antes de ter certeza de que vai instalar).

### Q6: resultado final, 29/10/2026 23:51 (Claude): OBSERVED, cobertura parcial
- Por orientação do Alexandre ("precisa derrubar o watchdog primeiro e depois o KyraFiles"): Watchdog → HotkeyService → instalador. Com isso o `KyraFiles.exe` PID 5960 deixou de aparecer e o instalador terminou: `exit code: 0`, `setup5.log` = "Installation process succeeded." (23:51:58). O instalador religou o Win+E sozinho; `verify_win_e_health.ps1` = `[OK]`.
- Configurações (OBSERVED): `user_settings.json` SHA256 `110608470889CFBEF3340360215D293B9FABA7C7A689693384C645212245D734` e `filetags.json` SHA256 `DF7CB6CF4FDC91C409E81FEC617B80A232B03BB497F0D09BDB80161E2A688A10`, idênticos antes e depois.
- Limite: foi reinstalação no mesmo caminho (`{app}` = caminho registrado), que não passa pelo `Remove-AppxPackage -PreserveApplicationData`. O caso original do bug (pacote registrado em outro caminho) continua NOT TESTED. O hash igual mostra que o caminho comum não apaga as configurações; não prova a correção do `.iss`.
- Causa provável do aborto anterior (INFERRED): o Watchdog supervisiona o `KyraFiles.exe` e o mantém "em uso" para o RestartManager; o `InitializeSetup` encerra tudo na mesma linha de `Stop-Process`, sem derrubar o Watchdog antes. Tarefa para a AGY: encerrar o Watchdog primeiro, esperar, depois o resto; e religar o Win+E se a instalação abortar.
- Uma execução (setup4) terminou com código -1073741205 sem criar log; não investigada.

- Segunda instalação por cima, 23:54:12 (setup6.log): exit 0, 'RestartManager found no applications using one of our files', hashes de user_settings.json e filetags.json idênticos, Win+E [OK]. Repetiu o resultado de 23:51 (OBSERVED, 2 de 2 com a ordem Watchdog, HotkeyService, instalador). O caso do pacote em outro caminho segue NOT TESTED.

