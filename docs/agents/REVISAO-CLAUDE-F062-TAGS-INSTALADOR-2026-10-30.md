# Revisão do Claude: F062, correção de tags e instalador (commit cd00407d3)

Data: 30/10/2026. Revisor: Claude. Autoria revisada: AGY. Decisão do Alexandre: Q14 do `QUESTIONARIO-DECISOES-2026-10-29.md`.
Método: leitura do diff de `cd00407d3` e dos arquivos atuais, mais execução do que roda sem tocar no app. Estados: `OBSERVED` = vi/rodei agora; `INFERRED` = deduzido; `NOT TESTED` = ninguém provou.

## Veredito
Nenhum defeito que impeça o uso. Há 3 pontos que pedem ação (A1 a A3) e 6 menores. Nada foi alterado no código da AGY.

## Ações recomendadas

**A1. "Perde as configurações toda vez que fecha" não foi explicado (NOT TESTED).**
O Alexandre relatou perda ao fechar *e* ao compilar/reinstalar. A causa levantada (`Remove-AppxPackage` sem `-PreserveApplicationData`, `.iss`) só explica a reinstalação por outro caminho. Perda ao *fechar* não tem causa nem prova. Em 29/10 o `user_settings.json` ficou com a data 08:42:58 de manhã até 23:54 (OBSERVED, `Get-ChildItem`); não sei se alguma configuração foi alterada nesse intervalo, então isso não prova nada sobre quando o app grava. Tarefa: reproduzir "mudar uma configuração, fechar, abrir" e comparar o arquivo; só então dizer que o bug está corrigido.

**A2. A tela Sobre vai mostrar um número que enganará depois da F063.**
`AboutViewModel.RefreshMemoryUsageAsync` resume só o Working Set total (`SettingsAboutEcosystemMemoryFormat` = "KyraFiles is using {0} across {1} active process(es)"). Com o trim da F063, o Working Set cai para ~4 MB enquanto o Private Bytes segue na casa de 200–300 MB (OBSERVED: 293 MB em 29/10 21:43). O AC-5 da F063 ("reflete fielmente") fica falso. Tarefa: mostrar Private Bytes no resumo, junto do Working Set (alinhado com a decisão Q3).

**A3. O instalador mata o Win+E e depois aborta (OBSERVED 3 vezes, 29/10).**
`InitializeSetup` encerra Watchdog, HotkeyService e Files numa linha só. Com o Watchdog vivo, o `KyraFiles.exe` permanece "em uso" para o RestartManager e a instalação aborta (exit 5), deixando o Win+E desligado. Com Watchdog → HotkeyService → instalador, 2 de 2 instalações terminaram com exit 0 (INFERRED a causa, OBSERVED o resultado). Tarefa: encerrar o Watchdog primeiro, esperar, depois o resto; religar o Win+E se a instalação abortar; e não esconder falha do `Add-AppxPackage` (hoje `-ErrorAction SilentlyContinue` e `ErrorCode` ignorado: um registro que falhe ainda diz "Installation process succeeded").

## Pontos menores

1. **Desinstalar apaga as configurações** (`usUninstall` chama `Remove-AppxPackage` sem `-PreserveApplicationData`). Pode ser a intenção; é decisão do Alexandre.
2. **Comparação de caminho no `.iss`** (`InstallLocation -ne {app}`) é por texto; barra final ou maiúscula diferente cai no ramo de remoção. Isso é seguro agora porque o ramo usa `-PreserveApplicationData`.
3. **Os testes não foram reproduzidos por mim.** O executável `KyraFiles.App.Search.Index.Tests.exe` existente é de 25/10 17:27 (anterior à F062): 60 testes, 59 passaram, 1 pulado, 0 falhas (OBSERVED, saída da execução). Ele não contém os 6 testes da F062 nem os 73 que a AGY cita. "6/6" e "73/73" ficam `NOT TESTED` por mim até recompilar os testes.
4. **Correspondência de processo só por nome** (`EcosystemMemoryDiagnostics.MonitoredProcessNames`). Outro programa chamado `KyraFiles.exe` (o KyraFiles oficial, por exemplo) seria somado ao total. Filtrar também pelo caminho do executável.
5. **Textos fixos em inglês**: `"Calculating..."` e `"0 MB"` no `AboutViewModel`, e as 15 linhas novas existem só em `en-US` (0 ocorrências em `pt-BR`, OBSERVED por `grep -c "Ecosystem"`). O Alexandre usa português.
6. **Tags**: a lógica `Contains("tag:")` está repetida em `EverythingSearchProvider`, `IndexedSearchProvider`, `ShellViewModel`, `NavigationToolbarViewModel` e `FolderSearch.IsTagQuery` (privado). Duplicação; o título mostra "Tags: Home" (recurso `FileTags` = "Tags"), não "Tag: Home" como o handoff afirma. O corte `Trim('"', ' ')` do nome da tag não desfaz o `""` duplicado de `FormatTagQuery` para nomes com aspas (borda).

## O que confere
- Filtros por tag: Alexandre relatou "testei e está ok" (29/10, resposta ao Q7). Estado: `OBSERVED` por ele, sem comando.
- `CollectSnapshot` roda em `Task.Run`, trata processo que sai durante a consulta (`HasExited` e `try/catch`) e não bloqueia a interface (leitura do código, OBSERVED).
- Instalação por cima no mesmo caminho preservou `user_settings.json` e `filetags.json` (SHA256 idêntico, 2 instalações, 29/10 23:51 e 23:54, `setup5.log` e `setup6.log`). O ramo "pacote em outro caminho", que é o do bug original, continua `NOT TESTED`.
