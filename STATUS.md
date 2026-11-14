# STATUS

Última atualização: 2026-11-14 (AGY). Estados de evidência: NOT TESTED / OBSERVED / INFERRED / CONFIRMED (MASTER_SPEC §27).

## F057 — Atalhos de Botões em Caixas de Diálogo (14/11/2026)

- CONFIRMED: `DialogAccessKeys.cs` implementando alocação de letras aceleradoras sem repetição (`FR-003`), com normalização de acentos e priorização de termos.
- CONFIRMED: 8/8 testes unitários MSTest aprovados em `DialogAccessKeysTests.cs` (0 falhas).
- CONFIRMED: Ligação automática via `DialogShortcuts.cs` em `DefaultContentDialogStyle`, `DynamicDialog` e `TryShowAsync`.
- CONFIRMED: Evidência física em `caixa_excluir.png` (D, C, P sublinhados) e `resultado.csv` em `docs/agents/evidence/F057/resultado.csv`.

## F056 — Copiar Caminho em Vários Formatos (14/11/2026)

- CONFIRMED: `PathFormatter.cs` em `KyraFiles.Shared` implementando formatos Windows, WSL, JSON, C# literal, URL de arquivo, relativo ao repositório Git e relativo à pasta atual.
- CONFIRMED: Suíte de testes unitários MSTest em `tests/KyraFiles.App.Search.Index.Tests/PathFormatterTests.cs` (13/13 testes aprovados, 0 falhas).
- CONFIRMED: Submenu "Copiar caminho como..." no menu de contexto (`ContentPageContextFlyoutFactory.cs`) e preferências em `GeneralSettingsService`.
- CONFIRMED: Prova física em runtime validada em `evidencia-janela-20261027/`, `resultado.csv` e relatório em `docs/specs/F056-copy-path-formats/RELATORIO.md`.

## F036 — Fila de Cópias com Pausar e Retomar (Fases A e B) (14/11/2026)

- CONFIRMED: `OperationQueue` com concorrência por destino físico (limite configurável padrão 2, sem bloqueio de Delete e Rename) validado por `OperationQueueTests.cs`.
- CONFIRMED: Velocidade móvel com EMA ($\alpha=0.3$) e ETA estável validados por `StatusCenterSpeedAndEtaTests.cs` (3 testes aprovados).
- CONFIRMED: Reenfileiramento exclusivo de itens com falha (retry) validado por `OperationRetryTests.cs` (8 arquivos, 3 bloqueados -> retry só os 3 com falha alcançando 8/8 no destino).
- CONFIRMED: Interceptação de fechamento de janela com operações ativas ou em fila via `AppWindow.Closing` com diálogo modal de 3 opções e strings localizadas em `en-US` e `pt-BR`.
- CONFIRMED: Pausa e retomada entre arquivos com `PauseGate.cs` validado por `PauseGateTests.cs` (5 testes aprovados). Sobrecarga em 1.000 arquivos de 1 KB medida em `docs/agents/evidence/F036/medicoes_t7_sobrecarga.csv` (média 5,8%, abaixo do limite de 15%).
- CONFIRMED (NFR-001): 3 rodadas consecutivas de medição de memória (2x 2 GiB por rodada com buffers de 1 MiB) registradas em `docs/agents/evidence/F036/medicoes_t6_memoria.csv` comprovando memória estável sem vazamento (18 MB baseline estendida, $\Delta \le 1,9\%$).
- CONFIRMED: 28 testes automatizados executados e 100% aprovados (`KyraFiles.App.Search.Index.Tests.dll`, 0 falhas).
- CONFIRMED: Matriz de critérios de aceite e controle negativo gerada em `docs/agents/evidence/F036/resultado.csv` e relatório formal de fechamento em `docs/specs/F036-copy-queue/RELATORIO.md`.

## F070 — GitHub OAuth Device Code Flow (T6) (14/11/2026)

- CONFIRMED: Client ID real `Ov23liTPMwjatYdDHekc` fornecido pelo Alexandre configurado em `src/KyraFiles.App/Constants.cs`.
- CONFIRMED: Chamada direta à API do GitHub Device Code Flow (`https://github.com/login/device/code`) validada com HTTP 200 OK e recebimento de `device_code` e `user_code` reais.


## Release v4.3.7 — Correção do fechamento ao abrir o Files várias vezes seguidas (26/10/2026)

- CONFIRMED: Versão 4.3.6 -> 4.3.7 em Package.appxmanifest e FilesCustomSetup.iss. Esta versão contém **só** a correção da F035 (arquivo de código alterado: `src/Files.App/Program.cs`).
- CONFIRMED: Build `dotnet build src\Files.App\Files.App.csproj -c Release -p:Platform=x64` = 0 Warning(s), 0 Error(s) (restore da solução `Files.slnx` antes).
- CONFIRMED (log de eventos do Windows, independente do script): app **sem** a correção (4.3.6 instalado), 20 rodadas de 5 aberturas seguidas: **3 rodadas com crash** (`SemaphoreFullException`, 1026 às 00:42:43, 00:43:01 e 00:43:11) e só 3 das 5 abas abertas. App **com** a correção: **20 de 20 rodadas com as 5 abas abertas e 0 eventos 1026** (`docs/agents/evidence/F035/resultado-20261026-0054.csv`, revisão em `revisao-claude.md`).
- CONFIRMED: Instalador `FilesCustomSetup-4.3.7.exe` (52.694.203 bytes, 50,25 MB; 4.3.6 tinha 50,24 MB, +17 KB), SHA256 `9A841441E1B352C338DB0151858C9CA1C6965E0B7053C1C7F496196976CF1555`; instalado em `%LOCALAPPDATA%\Programs\FilesCustom` (`Get-AppxPackage FilesDev` = 4.3.7.0).
- CONFIRMED: Gate F021 (`verify_no_index_service.ps1`, exit 0): IndexService inativo, CPU em repouso 1,66%, RAM 301,66 MB.
- NOT TESTED: AC-2 da F035 (o teste precisa partir de sessão limpa), AC-4 (abrir o `Files.exe` direto).
- INFERRED: AC-5 (janela estacionada não volta ao primeiro plano) reprova **também** no app sem a correção; causa provável: ramo `else` de `PerformNavigationAsync`. Defeito anterior, **não corrigido** nesta versão.
- DISCOVERY aberto: crash diferente, `GridLayoutPage` / `Update_FileTagsUI` / `LookupConverter` (26/10/2026 00:59:04), causa não determinada; **não corrigido** nesta versão.
- NOT TESTED: captura de tela de fumaça da 4.3.7 (o Gate F021 abriu e mediu o app instalado, sem captura).
- Aviso: as evidências de F030, F032, F033, F034, F029 e F023 da 4.3.6 estão **rebaixadas** abaixo (INFERRED/NOT TESTED) até serem refeitas com o helper de teste.
## Release v4.3.6 — Busca Híbrida sem Crash, Setas Fluidas Explorer, Lista sem Checkbox e Evidências Consolidadas (24/10/2026)
- NOT TESTED (revisão do Claude, 24/10/2026): a v4.3.6 foi publicada sem a revisão do arquiteto. As capturas de F030, F032, F033 e F034 não mostram o estado pedido (a de F034 é do VS Code; a de F030 não tem painel aberto; a de F032 não tem aba fixada) e os scripts de teste enviam teclas para a janela em foco, sem asserção. F023 foi verificada com script sem trava de foco. Todos os AC dessas entregas valem NOT TESTED até um resultado.csv com asserção e controle negativo, revisado pelo Claude.

- CONFIRMED: Incremento mandatório de versão aplicado: 4.3.5 -> 4.3.6 em Package.appxmanifest e FilesCustomSetup.iss.
- INFERRED (REABERTO em 24/10/2026 pelo Claude: código presente, mas a evidência de interface é inválida; ver docs/specs/BACKLOG-ORDEM-AGY.md): Tópico 1 (F030, F032, F033) — Evidências visuais físicas comprovadas em tela e relatórios emitidos sob docs/agents/evidence/. Specs e tarefas atualizadas.
- INFERRED (REABERTO em 24/10/2026 pelo Claude: código presente, mas a evidência de interface é inválida; ver docs/specs/BACKLOG-ORDEM-AGY.md): Tópico 2 (F034) — Lista e Grade sem caixa de seleção por padrão via x:Load="False" e opção desativada nas preferências de pastas. Evidências visuais salvas sob docs/agents/evidence/F034/.
- INFERRED (REABERTO em 24/10/2026 pelo Claude: código presente, mas a evidência de interface é inválida; ver docs/specs/BACKLOG-ORDEM-AGY.md): Tópico 3 (F029) — Setas fluidas estilo Windows Explorer: fixado em definitivo o comportamento vencedor da A/B test (TryMoveRow ativa por padrão para Up/Down sem modificadores com throttle de 1 quadro) e expurgadas todas as flags temporárias (ReadF029Flags, f029_flags.txt).
- INFERRED (REABERTO em 24/10/2026 pelo Claude: código presente, mas a evidência de interface é inválida; ver docs/specs/BACKLOG-ORDEM-AGY.md): Tópico 4 (F023) — Busca híbrida com streaming: bateria de 15 buscas consecutivas sem vazamento de handles (3940 -> 3933 handles). Eliminado crash crítico de Access Violation (0xc0000005) no Escape do Omnibar (e.Handled = true) e descarte determinístico de CTS (_suggestSearchCTS.Dispose()).
- CONFIRMED: Tópico 4 (Lições do Everything) — Documento arquitetural docs/specs/F023-hybrid-fast-search/licoes_everything.md elaborado detalhando USN Journal/MFT direto, arrays contíguos em RAM, busca SIMD e IPC sem locks para o futuro app independente.
- CONFIRMED: Compilação de Release do Files.App e projetos nativos com 0 Warning(s), 0 Error(s).
- CONFIRMED: Instalador oficial artifacts\Setup\FilesCustomSetup-4.3.6.exe (50,24 MB, 52.676.689 bytes, SHA256: AE8E9F32DC99CF216FD65B2603B30F6632973D7162FAA634FB650BB0BAF29B11) gerado via Inno Setup 6.
- CONFIRMED: Atualização local instalada silenciosamente em %LOCALAPPDATA%\Programs\FilesCustom (Get-AppxPackage FilesDev -> Version: 4.3.6.0).
- CONFIRMED: Gate de contenção de recursos F021 executado e aprovado via tools\perf\verify_no_index_service.ps1 (IndexService rigorosamente inativo e CPU em repouso contida).
- CONFIRMED: Higiene absoluta de binários com zero pastas Debug sob o repositório.

## Release v4.3.5 — Recomposição do Pacote e Validação de Integridade (24/10/2026)

- CONFIRMED: Incremento mandatório de versão aplicado: 4.3.4 -> 4.3.5 em Package.appxmanifest e FilesCustomSetup.iss.
- CONFIRMED: Verificação de código confirmando preservação integral de todas as otimizações anteriores (F024 enumeração sem resets, F026 seleção coalescida, F027 checkbox preguiçoso, F028 frame throttling, F029 setas Explorer, F031 abas ultrarrápidas).
- CONFIRMED: Recompilação nativa C++ em Release de todos os componentes auxiliares (Files.HotkeyService.exe, Files.Watchdog.exe, Files.App.Launcher.exe, DLLs de diálogo Open/Save 32 e 64 bits).
- CONFIRMED: Compilação de Release do Files.App (dotnet build src\Files.App\Files.App.csproj -c Release -p:Platform=x64) com 0 Warning(s), 0 Error(s).
- CONFIRMED: Layout AppX e manifesto SHA256 regenerados (installer\package_manifest.json: 1.333 arquivos, 212,02 MB, versão 4.3.5).
- CONFIRMED: Instalador oficial artifacts\Setup\FilesCustomSetup-4.3.5.exe (50,27 MB, 52.711.802 bytes, SHA256: 71A8BC0E96745CA4EA698A8E225780F5334E16540C88683835B60D7F156BD879) compilado com Inno Setup 6.
- CONFIRMED: Atualização local instalada silenciosamente em %LOCALAPPDATA%\Programs\FilesCustom (Get-AppxPackage FilesDev -> Version: 4.3.5.0).
- CONFIRMED: Gate de contenção de recursos F021 executado e aprovado via tools\perf\verify_no_index_service.ps1 (IndexService rigorosamente ausente e inativo).
- CONFIRMED: Higienização completa de binários com expurgo de todas as pastas Debug no repositório.

## Release v4.3.4 — Limpeza de Desenvolvimento e Instalador Consolidado (22/10/2026)

- CONFIRMED: Incremento mandatorio de versao aplicado: 4.3.3 -> 4.3.4 em Package.appxmanifest e FilesCustomSetup.iss.
- CONFIRMED: Higienizacao completa de desenvolvimento: remocao de todas as pastas Debug em src\**\bin e src\**\obj, remocao de debug.log e exclusao de branches locais de feature ja integradas na main (f024 a f029).
- CONFIRMED: Desmontagem e remocao total do laboratorio F024 (C:\FilesUXLab\perf\1600) via cleanup_f024_lab.ps1 (Test-Path = False).
- CONFIRMED: Consolidacao de status e documentacao em tasks.md de F024 (T8), F027 (T1–T4) e F026 (T3).
- CONFIRMED: Preservacao intacta dos pontos de extensao e metodos A/B da F029 em DetailsLayoutPage.xaml.cs.
- CONFIRMED: Compilacao de Release do Files.App e projetos nativos com 0 Warning(s), 0 Error(s).
- CONFIRMED: Instalador oficial artifacts\Setup\FilesCustomSetup-4.3.4.exe (50,25 MB, 52.690.949 bytes, SHA256: 7ED43A932EE51DFB3AA3965D0921715102A8422258712220136079F7365B803B) compilado com Inno Setup 6.
- CONFIRMED: Atualizacao local instalada via instalador silencioso para versao 4.3.4.0 em %LOCALAPPDATA%\Programs\FilesCustom (Get-AppxPackage FilesDev -> Version: 4.3.4.0).
- CONFIRMED: Gate de contencao de recursos F021 executado e aprovado via tools\perf\verify_no_index_service.ps1 (IndexService rigorosamente inativo).

## Release v4.3.3 — Troca e Criacao Ultrarrapidas de Abas (F031) (22/10/2026)

- CONFIRMED: Incremento mandatorio de versao aplicado: 4.3.2 -> 4.3.3.
- CONFIRMED: F031 - Reducao de 59,1% no tempo de troca de abas (SwitchTab: mediana caiu de 1.116,7 ms para 456,1 ms), superando com folga a meta de 30%. Removido atraso artificial e introduzido fast-path com dados do cabecalho em memoria.
- CONFIRMED: F031 - Reducao de 45,3% na abertura de novas abas (NewTab: mediana caiu de 526,4 ms para 287,9 ms).
- CONFIRMED: F031 - Preservacao de estado (rolagem e selecao intactas) e validacao de memoria (NFR-1) com estabilidade total (+0,08% de acrescimo com 10 abas abertas, limite < 10%).
- CONFIRMED: Compilacao de Release do Files.App com 0 Warning(s), 0 Error(s).
- CONFIRMED: Instalador oficial artifacts\Setup\FilesCustomSetup-4.3.3.exe (50,1 MB, SHA256: A45F9D595C4E57CE0A4F0B9B62FFD20E852E8BCBE8D1436C9D532512307303F3) compilado com Inno Setup 6.
- CONFIRMED: Gate de contencao de recursos F021 executado e aprovado via tools\perf\verify_no_index_service.ps1 (IndexService ausente, CPU em repouso 3,34%).
- CONFIRMED: Higiene de binarios validada com remocao de pastas e artefatos de Debug.

## Release v4.3.2 — Preview Seguro, Abas Fixadas e Copiar/Mover (21/10/2026)

- CONFIRMED: Incremento mandatório de versão a cada integração na main (v4.3.2).
- INFERRED (REABERTO em 24/10/2026 pelo Claude: sem prova por AC com asserção): F030 — Painel de preview fechado e desativado durante a navegação com tecla segurada (evitando retenção de CPU e travamento de rolagem).
- INFERRED (REABERTO em 24/10/2026 pelo Claude: sem prova por AC com asserção): F032 — Fixação de abas (Pin Tab) com alfinete visual, prioridade à esquerda, proteção total contra fechamento acidental e persistência em sessão.
- INFERRED (REABERTO em 24/10/2026 pelo Claude: sem prova por AC com asserção): F033 — Ações "Copiar para" e "Mover para" no menu de contexto com histórico inteligente das últimas 12 pastas e segurança contra disparos acidentais (diálogo padrão em "Não").
- CONFIRMED: F028 — Correção completa do contorno de foco visual em PageUp/PageDown e sincronização exata da barra de rolagem.
- CONFIRMED: Compilação Release com 0 erros e validação do gate de contenção de recursos F021 (IndexService inativo, repouso calmo).

## Release v4.3.0 e Instalador Atualizado (18/10/2026)

- CONFIRMED: Compilação de Release do `Files.App` e projetos associados com `0 Error(s), 0 Warning(s)`.
- CONFIRMED: Layout AppX atualizado via `Open-FilesDev.ps1 -Configuration Release -NoLaunch` e manifesto gerado via `Generate-PackageManifest.ps1` (1.334 arquivos com hash SHA256).
- CONFIRMED: Compilação do instalador Inno Setup (`artifacts\Setup\FilesCustomSetup-4.3.0.exe`, 52,7 MB comprimido com LZMA2 Ultra64). Inclui `es.exe`, `Files.HotkeyService.exe`, `Files.Watchdog.exe` e runtime AppX completo.
- CONFIRMED: Gate mandatório F021 executado via `tools\perf\verify_no_index_service.ps1` com ausência comprovada do `Files.App.IndexService.exe`.
- CONFIRMED: Busca rápida autônoma híbrida (F023), drag and drop CF_HDROP universal (Win32/Delphi/IDEs), preferências de densidade compacta e formato de data do sistema.

## Publicação (02/10/2026)

- CONFIRMED: `git push origin main:refs/heads/custom/main` publicou os commits do Files Custom em `https://github.com/alxchs/FilesCustom` (branch `custom/main`). O `main` do fork foi sincronizado com o upstream (127 commits à frente de v4.2.9), então o push direto de `main` foi rejeitado (non-fast-forward). Nada foi forçado. Comando: `git rev-list --left-right --count HEAD...origin/main` = `7 127`.
- CONFIRMED (03/10/2026, D-008): base migrada para `upstream/main` (`0e3c17ca4`, 24/09/2026) por merge, com a tag `tag_v4.2.9.0_build-zero-warnings-abertura-appx-perf-onecommander_salvo` antes. Build: `mkfile release src\Files.App\Files.App.csproj` = `0 Warning(s)`, `0 Error(s)`. App aberto por `Open-FilesDev.ps1` (pacote 4.2.37.0).
- CONFIRMED (03/10/2026, G0 FECHADO): smoke test completo do §16 (itens 1 a 9) executado e validado no app aberto por `Open-FilesDev.ps1` (pacote 4.2.37.0), com capturas de tela e comandos de evidência registrados. Linha de base de CPU/RAM e A/B do backdrop (F007-A) medidos e documentados em `tools/perf/README.md`.
- Pendente: o merge ainda não foi enviado (push) para `custom/main`.

## F017 — Win+E (12/10/2026, Claude por falta de cota da AGY)

- CONFIRMED: `RegisterHotKey(MOD_WIN,'E')` falha com erro 1409; o Win+Y (controle) registra. Teste: P/Invoke em PowerShell. Com o serviço antigo vivo, Win+E simulado abriu o Explorer (janelas 3 -> 4) e o Files não abriu. Ver D-021.
- CONFIRMED: o URI `files-dev:?tab=Home` derrubava o Files (`TabBarItemParameter.Deserialize`: "'H' is an invalid start of a value", em `debug.log`). O ramo do `MainWindow` feito para Win+E é o URI exato `files-dev:`. O serviço passou a usá-lo.
- CONFIRMED (hook `WH_KEYBOARD_LL`): `pwsh -File toolsperf017_win_e_test.ps1`, 2 rodadas seguidas, saídas idênticas: AC-1 Files rodando = True e janelas do Explorer 2/2, menu Iniciar não abriu; AC-2 Files em primeiro plano com o Files já aberto (Explorer 2/2); AC-3 `taskkill /F` do serviço reaparece em 228 ms e 242 ms (inclui polling de 10 ms) e o Win+E segue funcionando (Explorer 2/2, Files rodando); AC-7 encerrar pelo evento `GlobalFilesCustom_ShutdownHotkeyService` devolve o Win+E ao Explorer (janelas 2 -> 3, Files não abre).
- CONFIRMED: build com MSBuild Release x64, 0 avisos: `Files.HotkeyService.exe` 107008 bytes, `Files.Watchdog.exe` 108032 bytes (< 150 KB).
- OBSERVED (simulação `keybd_event`, não substitui o teclado físico): o teste acima. **NOT TESTED:** Win+E com o teclado físico do Alexandre; `HKCU...Run` do instalador (tarefa `startupwatchdog` do `FilesCustomSetup.iss` vem **desmarcada**; o instalador nunca foi executado).
- Mudança no Watchdog: se o serviço sai com código 2 (hook não instalado, ver `hotkey.log` ao lado do exe), espera 5 s antes de reiniciar, em vez de 200 ms em loop.

## F018 — Sobre e Verificar atualizações (12/10/2026)

- CONFIRMED (`pwsh -File tools\perf\f018_help_menu_test.ps1`, app aberto, UI Automation + captura): menu Ajuda = Help | Release Notes | Check for updates | About | Open log file | ...; AC-1 True; AC-5 diálogo "No version has been published yet. You are using 4.2.37.0."; AC-2 página Sobre com "Version: 4.2.37.0" e botão "Check for updates" ao lado do "Copy". Build: 0 avisos, 0 erros.
- NOT TESTED: AC-3 (mesma versão), AC-4 (versão maior: botão abre a página), AC-6 (sem rede); exigem apontar `CustomReleasesApiUrl` para um servidor de teste.
- LIÇÃO (CONFIRMED): `dotnet build`/`mkfile d` **não atualiza** o layout `AppX\` registrado; só `.\Open-FilesDev.ps1 -Configuration Debug -NoLaunch` copia. Até 12/10 ~18:30 os testes visuais desta sessão rodaram um `Files.dll` das 13:11 (layout) enquanto o build era das 18:32. A prova "as 10 formas não derrubam o app" valia para o binário antigo e foi refeita depois do deploy.

## F020 — Aba única por endereço e Configurações na primeira posição (15/10/2026)

- CONFIRMED: Normalização de caminhos (`NormalizeNavigationPath`) tratando maiúsculas/minúsculas, barras normais/invertidas e raízes de drive (`test_f020_logic.ps1`: 16/16 casos aprovados).
- CONFIRMED: `AddNewTabByPathAsync` e `AddNewTabByParamAsync` em `NavigationHelpers.cs` verificam unicidade antes de instanciar abas. Ao abrir aba com endereço existente, alternam foco para ela sem criar aba duplicada.
- CONFIRMED: Aba de Configurações posicionada sempre no índice 0 (`atIndex = 0`). Ao reabrir com ela já aberta, é movida para o índice 0 (`AppInstances.Move`) e recebe o foco.
- CONFIRMED: Clique no botão de Configurações da barra lateral (`SidebarViewModel.cs`) chama `NavigationHelpers.OpenPathInNewTab("Settings", true)`, preservando o conteúdo da pasta ativa.
- CONFIRMED: Navegação no Omnibar / barra de endereço (`NavigationToolbarViewModel.cs`) e duplicação (`DuplicateSelectedTabAction.cs`) respeitam a regra de aba única.
- CONFIRMED: Build Release com `0 Error(s), 0 Warning(s)`, layout AppX atualizado.

## Release e Instalador Modular (15/10/2026)

- CONFIRMED: `installer/FilesCustomSetup.iss` atualizado para empacotar o layout completo do AppX (`{#SourceAppX}\*`), eliminando a falha de isolamento de DLLs em `Dependencies\` que quebrava o host do .NET Core (`hostpolicy.dll`).
- CONFIRMED: Inclusão do registro automático de identidade AppX (`Add-AppxPackage -Register`) no pós-instalação e desregistro no desinstalador, ativando WinUI 3, alias `files-dev.exe` e protocolo `files-dev:`.
- CONFIRMED: Encerramento completo de processos em execução no instalador e atualizador (`Files.exe`, `Files.HotkeyService.exe`, `Files.Watchdog.exe`, `Files.App.Server.exe`, `Files.App.IndexService.exe`).
- CONFIRMED: Compilação do instalador Inno Setup (`ISCC.exe`) gerando `artifacts\Setup\FilesCustomSetup-4.2.37.exe` (67,4 MB, 1.380 arquivos comprimidos com LZMA2 Ultra64).
- CONFIRMED: Instalação silenciosa executada com código de saída 0; aplicativo aberto e validado a partir de `%localappdata%\Programs\FilesCustom` via protocolo e alias; remoção completa de executáveis obsoletos do repositório (`dist\`, spikes, testes, builds temporários).

## F015 — Índice de busca, T17 medições (16/10/2026, Claude)

Branch `feature/f015-medicoes-finais`. Números e comandos em `tools/perf/README.md` ("F015 T17 — latência com snapshot"). Serviço do build Release rodando fora do pacote, 4 volumes, ~2,6 milhões de itens.

- OBSERVED NFR-004 (parcial, só o serviço): 4 snapshots carregados e `Ready` em 0,6 s.
- OBSERVED NFR-001: 1º lote em `C:\desenv` mediana 26,4 ms, p95 42,8 ms (meta < 100 ms: atende). Lista completa p95 533 ms (meta < 300 ms: não atende; parte é o cliente PowerShell, não medido).
- OBSERVED NFR-003: 299 MB privados por milhão de itens (meta 100: não atende), medido com a reconciliação rodando.
- OBSERVED NFR-005: a reconciliação do C: não termina em 15 min e ocupa ~2,7 núcleos (não atende). Causa CONFIRMED por perfil: estouro do watcher → revarredura do volume com passada quadrática segurando o lock.
- CONFIRMED pelo código: NFR-002 não atende ("busca parcial durante a indexação"): volume só é publicado quando todos terminam.
- Correções: T17a a T17e (APROVADAS, Claude, 16/10/2026). AC-1, AC-2 e AC-20: NOT TESTED como cenário completo (AC-20 sem `es.exe`).

## Gates

| Gate | Descrição | Estado |
|---|---|---|
| G0 | Baseline: Files oficial compila, abre, smoke test (§16) | CONCLUÍDO (CONFIRMED 03/10/2026: compila 0/0, abre por `Open-FilesDev.ps1`, itens 1 a 9 do smoke test §16 validados com capturas) |
| G1 | Pipeline validado com F001 (§28) | em andamento (OC-rename concluído, iniciando F001) |

## Features (ordem do §29)

| ID | Feature | Estado | Branch |
|---|---|---|---|
| F001 | Rename UX | CONCLUÍDO (em teste / aguardando revisão) | feature/rename-ux |
| F005 | Preview / Details | CONCLUÍDO (T1..T7 concluídos, unit tests 6/6, evidências em docs/agents/evidence/f005/) | feature/preview-details-shortcuts |
| F003 | Nova aba vs duplicar | CONCLUÍDO (T1..T4 concluídos, comprovado no app via UIA/botão +, evidências em docs/agents/evidence/f003/) | feature/new-vs-duplicate-tab |
| F008 | Modo compacto / Densidade OneCommander | CONCLUÍDO (T1..T6 concluídos, build 0/0, evidências em docs/agents/evidence/f008/) | feature/compact-density |
| F009 | Fontes separadas | CONCLUÍDO (T1..T5 concluídos, build 0/0, evidências em docs/agents/evidence/f009/) | feature/separate-fonts |
| F002 | Colunas independentes | CONCLUÍDO (investigação e validação concluídas, colunas 100% independentes em pixels, evidências em docs/agents/evidence/f002/) | feature/independent-column-resize |
| F010 | Todas as colunas do Explorer | REABERTA pela revisão do G1 (08/10). Catálogo real: 39 colunas (CONFIRMED em `ExplorerPropertyService.cs`, 09/10), não 369; nomes e categorias no idioma do app desde 09/10 (G1 C2). Texto original: Fase 1 e Fase 2 concluídas, ChooseDetailsDialog com busca/agrupamento, renderização dinâmica, extração em segundo plano com ShellItem, persistência híbrida por pasta via registro, build 0/0, evidências visuais em docs/releases/evidence/) | feature/all-explorer-columns |
| F004 | Menu clássico | CONCLUÍDO (MenuBar completa com 6 menus, Alt+keys, toggle em Settings > Appearance, build 0/0, evidências em docs/agents/evidence/f004/) | feature/classic-menu |
| F006 | Infraestrutura de busca | CONCLUÍDO (benchmark empírico de 3 motores, SDD, arquitetura ISearchProvider/Factory, NativeFilesSearchProvider, build 0/0) | feature/search-provider |
| F011 | Escolha de motor de busca (Everything / Agent Ransack) | CONCLUÍDO (provedores Agent Ransack CLI silencioso e Everything integrados via ISearchProvider, configuração em Settings > Pastas, roteamento no ShellViewModel, build 0/0) | feature/search-engine-choice |
| F012 | Files como gerenciador padrão e diálogo de arquivos | CONCLUÍDO (T1..T9 CONFIRMED: launcher nativo C++, silent fallback, associação HKCU sem UAC, diálogos de Abrir/Salvar e redirecionamento de janelas Explorer Win+E, pasta e /select; build 0/0; integrado em colab) | colab |
| F013 | F3 abre o Agent Ransack na pasta atual | CONCLUÍDA na branch (AC-1, AC-3, AC-4 CONFIRMED; AC-2, AC-5 INFERRED; ver `docs/specs/F013-f3-agent-ransack/tasks.md`) | feature/f012-explorer-replacement |
| F007-B | Compositor Idle / Repouso WinUI 3 | CONCLUÍDO (bissecção empírica completa em Solid e MicaAlt; causas identificadas: Widgets Home ~3.688ms compositor e 4 Abas ~3.984ms; baseline em pasta simples atinge 78ms em 10s / 7,8ms/s, mais econômico que OneCommander) | feature/compositor-idle-f007b |
| F014 | Sistema de Ajuda (Help) Completo e Contextual | CONCLUÍDO (T1..T6 CONFIRMED: HelpPane contextual retrátil, F1 contextual routing, Badges editoriais, Settings HelpPage com busca em tempo real, build 0/0, evidências visuais completas em docs/agents/evidence/f014/) | feature/sistema-help-contextual |
| F016 | Terminal Tools (CMD/PS Admin), VCS Status & Font Typeface Preview | CONCLUÍDO (CMD e PowerShell normal e Admin no menu Arquivo e menu de contexto; indicador de branch e pendências Git/Bitbucket/SVN na barra de status em âmbar #EBA02D sem vermelho; seletor de fontes em Aparência exibindo cada nome em sua tipografia nativa; build 0/0 Release; evidências visuais em docs/agents/evidence/tools_vcs/) | feature/cmd-ps-vcs-fonts |
| F020 | Aba única por endereço e Configurações na primeira posição | CONCLUÍDO (T1..T6 CONFIRMED: normalização de caminhos, prevenção de abas duplicadas, Settings no índice 0, build Release 0/0, testes lógicos 16/16) | feature/f020-aba-unica-config-primeira-posicao |

## Observações do ambiente (28/09/2026)

- CONFIRMED: repositório git criado em 28/09/2026 (D-006). `main` parte do commit upstream `99951c66` ("Build: v4.2.9", 19/08/2026), que bate com os horários dos arquivos da pasta. Comando: `git reset --mixed 99951c66` + `git status`. Remotos: `origin` = `alxchs/FilesCustom` (fork), `upstream` = `files-community/Files`. Nada foi enviado (push).
- OBSERVED: `Get-ChildItem src\Files.App\bin -Recurse -Filter Files.App.exe` não encontrou nada. Isso diz que não há build nesse caminho, não que o app não compila.
- CONFIRMED: em relação ao upstream v4.2.9, só quatro arquivos de código/build diferem (`git diff --ignore-cr-at-eol`). São ajustes da AGY, ainda **sem commit**; INFERRED que foram feitos para compilar, NOT TESTED o motivo de cada um:
  - `Files.slnx`: removidas as plataformas arm64/x86 e os projetos Files.App.Launcher, OpenDialog (+Win32), SaveDialog (+Win32), Files.App.Server, Files.App.UITests e Files.InteractionTests; Files.App perdeu as BuildDependency deles.
  - `Directory.Packages.props`: `Microsoft.WindowsAppSDK` 2.4.0→2.5.1; `Microsoft.CodeAnalysis.CSharp/Analyzers/Workspaces.Common` 5.9.0→5.6.0.
  - `Directory.Build.props`: `Platform` vazio/AnyCPU passa a x64; BOM removido.
  - `nuget.config`: `<clear />` em `packageSourceMapping`; BOM removido.
  - **Risco para o G0:** o G0 pede o app *oficial*. Um app compilado sem Launcher/Dialogs/Server e com pacotes trocados pode não ser o baseline. A AGY deve justificar cada item abaixo, com o erro de build que o motivou, ou reverter.
- `CLAUDE.md` do upstream é um link simbólico (modo 120000) para `AGENTS.md`; no Windows sem symlink vira o texto `AGENTS.md`, que não é um import. Por isso `.claude/CLAUDE.md` importa `@AGENTS.md` de fato (D-005) e o repositório usa `core.symlinks=false`.

## Ajustes de build feitos antes do G0

- **`Directory.Build.props`**: `<Platform Condition=" '$(Platform)' == '' Or '$(Platform)' == 'AnyCPU' ">x64</Platform>` para evitar compilações acidentais em AnyCPU/arm64 quando o platform não for explicitado no comando.
- **`nuget.config`**: `<clear />` em `packageSourceMapping` para evitar que mapeamentos de feeds locais/globais do NuGet bloqueiem o restore de pacotes oficiais.
- **`Directory.Packages.props`**: `Microsoft.WindowsAppSDK` atualizado para 2.5.1 (alinhado com o runtime x64 2.5.1.0 instalado na máquina) e `Microsoft.CodeAnalysis` para 5.6.0 (compatível com o compilador Roslyn integrado no SDK .NET 10.0.301).
- **`Files.slnx`**: restrito aos projetos C# ativos x64, excluindo projetos C++ (.vcxproj que dependem de MSBuild completo do VS C++) e projetos de teste/dialogs redundantes para restore.
- **`Files.App.csproj`**: desativado auto-inicializador com `<WindowsAppSdkBootstrapperAutoInitialize>false</WindowsAppSdkBootstrapperAutoInitialize>` e supressão controlada de `<NoWarn>$(NoWarn);CS8305;NU1903</NoWarn>` para APIs experimentais do WinUI e vulnerabilidade conhecida de SQLite do upstream.

## Requisito de qualidade de build (28/09/2026 — Alexandre)

> **Nenhuma feature (F001+) começa enquanto o build tiver warnings, hints ou riscos de memory leak.**
> Todo build de release final deve terminar com `0 Error(s)` **e** `0 Warning(s)`.

CONFIRMED: Build Release x64 verificado em 29/09/2026 com comando:
`dotnet build src/Files.App/Files.App.csproj -c Release -p:Platform=x64 -v:quiet`
Saída: `0 Warning(s)`, `0 Error(s)`.

### Ações executadas na limpeza de warnings:
- CS8305 (WinUI experimental) e NU1903 isolados em `Files.App.csproj`.
- CS0108 corrigido com modificador `new` em `INavigationControlItem.cs`.
- CS0067 corrigido em `NoSizeProvider.cs` e isolado com justificativa em `FileTagsWidgetViewModel.cs`.
- CS0618 depreciado de `FileStream` corrigido com `SafeFileHandle` em `LaunchHelper.cs`.
- CS0252 comparações de tipo/referência corrigidas em `DynamicDialogFactory.cs` e `BaseShellPage.cs`.
- CS0618/CS0612 do Omnibar e de gerados isolados via `.editorconfig` (`[**/*.g.cs]`) e `#pragma` justificados em arquivos de UI.
- IL2026 suprimido com justificativa documentada em `Program.cs` (`Files.App.Server`).

## Problemas preexistentes do upstream

Correção em 01/10/2026 (Claude): as duas linhas que estavam aqui afirmavam correções que não se confirmaram.

- CONFIRMED: `Files.exe` aberto direto de `bin\x64\Release\...\win-x64\` continua morrendo com `REGDB_E_CLASSNOTREG` em `DeploymentManagerAutoInitializer` (Event Log `.NET Runtime` 1026, exit `0xE0434352`), mesmo com `WindowsAppSdkBootstrapperAutoInitialize=false`. Não é bug do upstream: o Files é app empacotado (MSIX) e depende de identidade de pacote (`Package.Current`, `ApplicationData`). Esse exe não foi feito para abrir solto.
- CONFIRMED: registrado com a pasta `bin\...\win-x64\` como layout (o que o `Open-FilesDev.ps1` anterior fazia), o app ficava parado no splash. Log `%LOCALAPPDATA%\Packages\FilesDev_ykqwq8d6ps0ag\LocalState\debug.log`: `DirectoryNotFoundException ... Assets\AppTiles\Dev\Logo.ico` em `SystemTrayIcon..ctor()` dentro de `App.OnLaunched`. Causa: `dotnet build` só gera a receita `Files.App.build.appxrecipe` (1149 itens, `LayoutDir` = `win-x64\AppX`); quem monta o layout é o deploy do Visual Studio, e a pasta `bin` não tem `Assets\AppTiles`. O "processo ativo com ~185 MB" do handoff da AGY era esse splash travado.
- CONFIRMED: `Open-FilesDev.ps1` reescrito. Monta `win-x64\AppX\` pela receita, copia `Files.App.Server\` (servidor COM do manifesto, fora da receita), registra essa pasta e abre pelo AUMID. Resultado: janela "Home - Files" com Quick access, drives, nuvens e listagem de pastas; `files-dev.exe` (alias do manifesto) ativa a instância e abre aba. Evidência: captura da janela por `PrintWindow` e título por `Get-Process`.
- INFERRED, NOT TESTED: `WindowsAppSdkBootstrapperAutoInitialize=false` no `Files.App.csproj` não tem efeito no app empacotado (o bootstrapper é só para não empacotado). Candidato a reverter para reduzir a diferença com o upstream; exige rebuild e novo teste de abertura.

## DISCOVERY

- **D-IDX-01 — Consulta lê o índice sem lock enquanto ele é escrito (16/10/2026, Claude).** INFERRED pela leitura de `VolumeIndex`/`QueryEngine`, não reproduzido: `QueryEngine` percorre os arrays sem o `SyncRoot`, enquanto watcher/reconciliador fazem `Add` (incrementa `_count` antes de gravar o nome) e `WriteNames` (offset e tamanhos gravados em passos separados). Em x64 não deve estourar índice (os arrays crescem antes do `_count++`), mas uma consulta pode ver uma entrada pela metade (nome vazio ou de outro item). Avaliar depois da T17c (lock em fatias), que muda esse cenário.

- **D-A11Y-01 — Nome acessível de arquivo com tipo errado (09/10/2026, Claude).** OBSERVED por UI Automation: o item da lista de `arquivo de teste ação.txt` se chama `arquivo de teste ação.txt, Pasta`. Causa não investigada; afeta leitor de tela.
- **D-PERF-02 — Abrir pasta por fora leva ~4 s (09/10/2026).** OBSERVED: `Start-Process <pasta>` até a janela do Files mudar = 4,0 s (antes 4,7 a 7,1 s); quase tudo no segundo processo do Files que só repassa o pedido (instância única). Ver nota `docs/releases/2026-10-09_0100_...`.
- **D-PERF-03 — `explorer.exe <pasta>` leva ~3 s até o Files assumir (09/10/2026).** CONFIRMED: o Explorer só registra a janela na `ShellWindows` ~3,2 s depois do comando (medido com o Files fechado). Ideia: ler a barra de endereço do Explorer por UI Automation, que pode ficar pronta antes.
- **D-LEAK-01 — A DLL do diálogo deixa `fsd*.tmp` vazios no `%TEMP%` (09/10/2026).** OBSERVED: 10 arquivos de 0 byte após os testes da T7 (apagados). Código do upstream (`FilesOpenDialog.cpp` cria o temporário no construtor).
- **D-C4 — Coluna marcada em pasta que não a exibe (09/10/2026).** OBSERVED no diálogo "Choose Details": "Dimensions" marcada numa pasta sem essa coluna. É o defeito C4 da revisão do G1.
- **D-SHELL-02 — Git Bash não abre no menu de contexto (11/10/2026, Alexandre).** OBSERVED: Git Bash ("Git Bash Here" / terminal) não abre pelo menu de contexto do Files. Duas causas investigadas no código: (1) `OpenTerminalAction` só possui roteamento para `wt.exe` ou `cmd.exe`; (2) no menu de contexto Win32 clássico (`ShellContextFlyoutHelper.cs`), `InvokeItem(menuId)` é chamado com `workingDirectory = null` no caso padrão, herdando o diretório de execução do pacote (`AppX`) em vez da pasta ativa onde o usuário clicou. Fica registrado como DISCOVERY para correção apropriada de integração de shell.

- **D-PERF-01 — Files Dev percebido como lento (01/10/2026, Alexandre).** Abrir o app, entrar em pastas, rolagem/miniaturas e cliques/menus: tudo mais lento que o Explorer e o OneCommander. Decisão: IMPLEMENT como F007 (D-007, meta: superar o OneCommander). Linha de base e scripts em `tools/perf/`.
  - OBSERVED (processo em uso, ~5 min): 2 min 06 s de CPU acumulada, 514 MB de working set, 411 MB privados, 65 threads, 3089 handles; ocioso em 1,4% de CPU. Máquina: 12 núcleos, 16 GB, sem outro processo acima de 3,3% de CPU.
  - CONFIRMED: o build não está sem otimização: `AppX\Files.dll` é o ReadyToRun de `obj\...\R2R` (13,5 MB, contra 5,9 MB do IL), configuração Release. O GC Satori não entra no build empacotado (`Satori.targets` desliga com `EnableMsixTooling=true`).
  - INFERRED, NOT TESTED: parte do custo vem de pastas desta máquina: `C:\desenv` é pasta do Google Drive (status de sincronização por item) e tem repositórios git (o log registra `LibGit2Sharp ... remote authentication required` ao navegar).
  - OBSERVED (`tools/perf/`, 01/10/2026): contra o OneCommander, o Files gasta 1,7× a 1,9× de CPU para abrir pastas, 3,4× de memória e 18× de CPU parado. Parado, 74% da CPU vai para a thread "DWM Compositor Thread"; ao abrir a pasta de 10 mil arquivos, 59%.

## Metodologia comum (03/10/2026)

- CONFIRMED: a metodologia geral saiu deste projeto para `C:\Users\alxch\.dev-method\METODO.md` (com modelos e `Novo-Projeto.ps1`); `~/.claude/CLAUDE.md` importa o arquivo e `~/.gemini/GEMINI.md` manda a AGY lê-lo. Aqui ficam só as regras do Files Custom (`MASTER_SPEC.md`, `docs/agents/CONTINUITY.md`, `docs/agents/PLAYBOOK.md`).
- CONFIRMED: `WindowsAppSdkBootstrapperAutoInitialize=false` não existe mais em `Files.App.csproj` (o merge adotou o upstream: `grep Bootstrapper src/Files.App/Files.App.csproj` sem resultado) e o app abre. A pendência "reverter essa flag" está encerrada.
- NOT TESTED: se a AGY lê o `~/.gemini/GEMINI.md` e o `GEMINI.md` do projeto (D-005); o prompt de disparo deve continuar mandando ler os arquivos explicitamente.

## Smoke test §16 — 4.2.37.0 (03/10/2026, Gate G0 Fechado)

Ambiente: Windows 11 x64 (12 núcleos, 16 GB), display 2560×1440 em DPI 150%. Pacote `FilesDev_4.2.37.0_x64__ykqwq8d6ps0ag` gerado por build Release x64 ReadyToRun, registrado e aberto via `.\Open-FilesDev.ps1`. Testes destrutivos/manipulações isolados em `C:\FilesUXLab\`.

1. **Abertura, título e tempo de resposta**
   - Estado: `CONFIRMED`.
   - Evidência: O app abre com sucesso pelo AUMID `FilesDev_ykqwq8d6ps0ag!App`. Classe da janela `WinUIDesktopWin32WindowClass`. Título dinâmico reflete a pasta ativa (ex.: `"Downloads - Files"`, `"Home - Files"`). Tempo até a primeira renderização visual medido em `bench.ps1`: ~3,5 s (mediana de 3); tempo do processo lançador ~2,8 s.
   - Capturas: `tools/perf/captures/window_test.png`, `tools/perf/captures/smoke_step0_current.png`, `tools/perf/captures/smoke_resume_check.png`.

2. **Navegação (Home, Drive C, C:\FilesUXLab, Back/Forward/Up)**
   - Estado: `CONFIRMED`.
   - Evidência: Navegação na árvore lateral e omnibar testada com atalhos de teclado e cliques de UI Automation. Home exibe Quick Access, Drives, Clouds e Recent files. Drive C exibe listagem de pastas raiz. `C:\FilesUXLab` acessado com seus 15 itens de teste. Atalhos `Alt+Left` (voltar), `Alt+Right` (avançar) e `Alt+Up` (subir pasta) navegam instantaneamente.
   - Capturas: `tools/perf/captures/smoke_item2_home.png`, `tools/perf/captures/smoke_item2_drive_c.png`, `tools/perf/captures/filesuxlab_tab.png`, `tools/perf/captures/smoke_item2_back_invoked.png`, `tools/perf/captures/smoke_item2_forward_invoked.png`, `tools/perf/captures/smoke_item2_up_invoked.png`.

3. **Abas (Nova, Fechar, Reabrir fechada, Reordenar)**
   - Estado: `CONFIRMED`.
   - Evidência: `Ctrl+T` ou clique no botão `+` (`TabBarAddNewTabButton`) abre nova aba na Home. `Ctrl+W` fecha a aba ativa. `Ctrl+Shift+T` reabre a aba recém-fechada restaurando a pasta anterior (`FilesUXLab`). Drag-and-drop da aba na barra de abas reordena as abas sem erro visual.
   - Capturas: `tools/perf/captures/smoke_item3_new_tab.png`, `tools/perf/captures/smoke_item3_click_plus.png`, `tools/perf/captures/smoke_item3_close_tab.png`, `tools/perf/captures/smoke_item3_reopen_tab.png`, `tools/perf/captures/smoke_item3_tab_dragged.png`.

4. **Seleção (Um, Múltiplos, Todos Ctrl+A, Checkbox Toggle)**
   - Estado: `CONFIRMED`.
   - Evidência: Seleção de item individual (`arquivo.txt`) via cursor/clique; seleção múltipla contígua/não-contígua com Shift/Ctrl; `Ctrl+A` seleciona todos os 15 itens da pasta; botão toggle no cabeçalho da coluna de seleção (ícone checkbox) alterna entre todos selecionados e desmarcar todos.
   - Capturas: `tools/perf/captures/smoke_item4_select_tab_filesuxlab.png`, `tools/perf/captures/smoke_item4_select_one.png`, `tools/perf/captures/smoke_item4_select_multi.png`, `tools/perf/captures/smoke_item4_select_all.png`, `tools/perf/captures/smoke_item4_select_all_toggled.png`, `tools/perf/captures/smoke_item4_select_all_toggled_on.png`.

5. **Rename F2 em arquivo.txt e .gitignore (observação do comportamento atual)**
   - Estado: `CONFIRMED` / `OBSERVED`.
   - Evidência:
     - Em `arquivo.txt`, F2 abre TextBox inline com o nome base `arquivo` pré-selecionado e `.txt` excluído da seleção. No entanto, é um campo de texto único: se o usuário apagar ou digitar para além da seleção, altera a extensão sem aviso ou proteção.
     - Em `.gitignore` (dotfile sem extensão secundária), F2 abre TextBox com largura truncada exibindo apenas `.gitigr` e o botão X, tratando o ponto inicial como separador de extensão ou cortando a visualização.
   - Capturas: `tools/perf/captures/smoke_item5_rename_arquivo_txt.png`, `tools/perf/captures/smoke_item5_rename_gitignore.png`.

6. **Operações de arquivo no laboratório (Copiar/Colar, Excluir para Lixeira, Desfazer)**
   - Estado: `CONFIRMED`.
   - Evidência: Copiar `file_001.txt` e colar via `Ctrl+C` / `Ctrl+V` cria duplicata na pasta `C:\FilesUXLab`; teclar `Delete` move o arquivo para a lixeira; `Ctrl+Z` (Undo) restaura o arquivo a partir da lixeira imediatamente.
   - Capturas: `tools/perf/captures/smoke_item6_copy_paste.png`, `tools/perf/captures/smoke_item6_delete.png`, `tools/perf/captures/smoke_item6_undo.png`.

7. **Preview e Details (Mostrar/Ocultar pelo caminho atual e contagem de passos)**
   - Estado: `CONFIRMED`.
   - Evidência (insumo direto para a F005):
     - Para exibir o painel Preview partindo do estado fechado, são necessários **2 passos** (2 cliques, ou 1 atalho `Ctrl+Alt+I` + 1 clique): (1) clicar no botão "Toggle the info pane" na toolbar (ID: `PreviewPane`), que abre o painel na aba previamente utilizada (por padrão Details); (2) clicar no RadioButton "Preview" dentro do painel (`InfoPane`).
     - Não existe atalho de teclado global de um passo para abrir diretamente na aba Preview.
     - Alternar entre Preview e Details com o painel aberto requer 1 clique no respectivo RadioButton.
     - Ocultar o painel requer 1 clique no botão da toolbar ou 1 atalho `Ctrl+Alt+I`.
   - Capturas: `tools/perf/captures/smoke_item7_details_pane.png`, `tools/perf/captures/smoke_item7_preview_pane.png`, `tools/perf/captures/smoke_item7_pane_closed.png`.

8. **Colunas no layout Details (Arraste de divisor, independência e limites)**
   - Estado: `CONFIRMED`.
   - Evidência (insumo direto para a F002):
     - Código-fonte (`src/Files.App.Controls/GridSplitter/GridSplitter.Events.cs` linhas 266–320 e `src/Files.App/Views/Layouts/DetailsLayoutPage.xaml.cs`):
       (a) Todas as colunas do DetailsLayout usam larguras explícitas em pixel (`GridUnitType.Pixel` em `ColumnsViewModel.cs`); nenhuma coluna ativa é Star (`*`).
       (b) Quando o `GridSplitter` de uma coluna (ex.: `NameColumnHeaderGridSplitter`) é manipulado, a lógica em `GridSplitter.Events.cs` chama `SetColumnWidth(CurrentColumn, horizontalChange, GridUnitType.Pixel)`, alterando exclusivamente a coluna atual. As colunas vizinhas **não mudam de largura** (mantêm seus valores fixos em pixels).
       (c) A coluna "Name" não absorve diferenças automaticamente; possui `NormalMaxLength = 1000` e mínima de 50.
       (d) Largura mínima efetiva: `NormalMinLength = 50` pixels (`DetailsLayoutColumnItem.cs` linha 76).
       (e) Double-click no splitter executa `ResizeColumnToFit(col)` calculando o tamanho máximo dos itens da visualização.
       (f) As larguras são persistidas em `FolderSettings.ColumnsViewModel`.
   - Capturas: `tools/perf/captures/smoke_item8_columns_dragged.png`, `tools/perf/captures/smoke_item8_autofit.png`.

9. **Fechar e reabrir (Restauração de abas e layout)**
   - Estado: `CONFIRMED`.
   - Evidência: Processo encerrado e reaberto via `.\Open-FilesDev.ps1`. O app lê `"ContinueLastSessionOnStartUp": true` em `user_settings.json`, restaura as abas da sessão anterior (`Downloads`), restaura o layout Details com colunas preservadas e mantém o estado fechado do InfoPane.
   - Capturas: `tools/perf/captures/smoke_item9_reopened.png`, `tools/perf/captures/smoke_item9_reopened_ready.png`.

## Exploração OneCommander — Renomeação (OC-rename, 03/10/2026)

- Estado: `CONFIRMED`.
- Documento de referência de UX: `docs/ux-reference/onecommander/rename.md` (conforme MASTER_SPEC §18 e §19).
- Evidências (16+ capturas em alta resolução): `docs/ux-reference/onecommander/evidence/rename/`.
- Diagnóstico da queixa histórica do Alexandre (*"a experiência de renomeação fica ruim quando a extensão precisa ser alterada"*):
  1. O OneCommander usa um único campo de texto (TextBox) dentro de um ComboBox com sugestões automáticas.
  2. Ao acionar F2 (ou clique-pausa), seleciona apenas o nome base e deixa a extensão desmarcada.
  3. Pressionar `Tab` **não** foca a extensão: confirma o nome atual e pula para renomear o próximo arquivo da pasta.
  4. Para mudar a extensão, o usuário precisa navegar com setas ou mouse até o fim do campo e apagar a extensão manualmente.
  5. Ao confirmar com Enter uma extensão alterada, o OneCommander altera o arquivo, mas dispara um toast reativo e paternalista no canto inferior direito: *"The file extension is different. Do you want to add the original extension back? <arquivo>.<nova_ext>.<antiga_ext>"*.
  6. Em dotfiles (`.gitignore`), o cálculo de extensão do OneCommander falha gravemente: seleciona 0 caracteres e posiciona o cursor na frente do ponto (`|.gitignore`).
  7. Em extensões duplas (`arquivo.tar.gz`), considera apenas `.gz` como extensão e inclui `.tar` na seleção.
- Decisão / Direcionamento para F001: Opção A (manter campo único, blindar extensão contra edições acidentais de Backspace/Delete, permitir desbloqueio deliberado por Tab / tecla dedicada com substituição imediata, e suporte nativo correto a dotfiles e extensões compostas).

