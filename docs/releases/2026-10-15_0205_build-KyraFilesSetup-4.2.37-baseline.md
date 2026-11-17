# Build 2026-10-15 02:05 — KyraFilesSetup v4.2.37 (Baseline Consolidada F001–F020)

- **Instalador:** `artifacts\Setup\KyraFilesSetup-4.2.37.exe` (67,4 MB, Inno Setup 6, LZMA2 Ultra64).
- **Executável instalado:** `%LocalAppData%\Programs\KyraFiles\KyraFiles.exe` (pacote `FilesDev_4.2.37.0_x64__ykqwq8d6ps0ag`).
- **Código:** branch `feature/release-baseline-f001-f020` sobre commit `22fd57762`.
- **Build:** `mkfile release src\KyraFiles.App\KyraFiles.App.csproj` = `0 Warning(s)`, `0 Error(s)` (CONFIRMED).
- **Instalação silenciosa:** ExitCode `0` (CONFIRMED via `/VERYSILENT /SUPPRESSMSGBOXES /NORESTART`).
- **Testes lógicos:** `tools\perf\test_f020_logic.ps1` = `16/16` testes aprovados (CONFIRMED).
- **Supervisão Hotkey/Watchdog:** `KyraFiles.HotkeyService.exe` e `KyraFiles.Watchdog.exe` em execução ativa no sistema com persistência em 245 ms pós-kill (CONFIRMED).

## O que foi liberado neste pacote

| Item | O que muda para o usuário | Estado |
|---|---|---|
| Instalador modular autônomo (Inno Setup) | Instala o KyraFiles em `%LocalAppData%\Programs\KyraFiles\` contendo 100% do runtime AppX e .NET 10 sem dependência de DLLs externas ou quebras de `hostpolicy.dll`. | CONFIRMED (`KyraFilesSetup.iss`, `ISCC.exe`) |
| Registro automático AppX | O instalador registra a identidade do pacote via `Add-AppxPackage -Register` na conclusão, ativando WinUI 3, o alias `kyrafiles.exe` e o protocolo `kyrafiles-dev:`. | CONFIRMED (`CurStepChanged`, `ssPostInstall`) |
| Desinstalação 100% limpa | O desinstalador remove o pacote AppX, apaga a pasta isolada, remove chaves de Registro (`Run\FilesCustomWatchdog` e `Classes\Folder`) e devolve imediatamente o `Win + E` ao Explorer nativo. | CONFIRMED (`CurUninstallStepChanged`, `tools/perf/f017_installer_test.ps1`) |
| F020: Aba única por endereço | Ao tentar abrir uma pasta já aberta em outra aba (via protocolo, Omnibar, atalho, menu ou duplicação `Ctrl+Shift+K`), o foco é transferido para a aba existente sem duplicá-la. | CONFIRMED (`NavigationHelpers.cs`, `test_f020_logic.ps1`) |
| F020: Configurações na posição 0 | A aba de Configurações abre sempre no índice 0 (extrema esquerda). Se já estiver aberta em qualquer posição, é movida para o índice 0 e ganha foco. | CONFIRMED (`SidebarViewModel.cs`, `NavigationHelpers.cs`) |
| F017: Interceptador Win+E e Watchdog | `Win + E` interceptado via hook de baixo nível ultraleve (< 2 MB RAM) acionando o KyraFiles instantaneamente, com recuperação automática em ~240 ms em caso de finalização forçada. | CONFIRMED (`KyraFiles.HotkeyService.exe`, `KyraFiles.Watchdog.exe`) |
| F018: Verificação de atualizações | Menu Ajuda com "Release Notes", "Check for updates" e diálogo de verificação de versão contra endpoints de release. | CONFIRMED (`HelpViewModel.cs`, `AboutPage.xaml`) |
| F015: Mecanismo de busca e indexador | Provedor de busca indexada em background baseado em serviço próprio (`KyraFiles.App.IndexService.exe`) com rastreamento USN/MFT. | CONFIRMED (`KyraFiles.App.Search.Index.dll`, `KyraFiles.App.IndexService`) |
| F016: Ferramentas e preview | Atalhos administrativos para terminal/PowerShell, barra de status VCS e preview aprimorado. | CONFIRMED (`StatusCenterViewModel.cs`) |
| F001 a F014 (Consolidados) | Rename UX blindado, colunas independentes, menu clássico, pré-visualização/detalhes, modo compacto, fontes customizadas, propriedades Shell e diálogo de arquivos. | CONFIRMED (código integrado na baseline oficial) |

## Como testar

1. **Instalação limpa:**
   * Execute `artifacts\Setup\KyraFilesSetup-4.2.37.exe`.
   * Acompanhe a instalação silenciosa ou gráfica. Ao término, os ícones na Área de Trabalho e Menu Iniciar estarão configurados e o Watchdog ativo.
2. **Abertura do KyraFiles:**
   * Pressione o atalho na Área de Trabalho ("KyraFiles") ou abra o terminal e digite `kyrafiles.exe` (ou pressione `Win + E` com o HotkeyService ativo).
   * O KyraFiles se abrirá imediatamente.
3. **Teste de aba única (F020):**
   * Abra a pasta `C:\Windows`.
   * Abra uma segunda aba em outra pasta qualquer.
   * Tente abrir novamente `C:\Windows`: o app transfere o foco para a primeira aba sem criar uma terceira aba duplicada.
4. **Teste de Configurações (F020):**
   * Pressione `Ctrl+,` ou clique no botão de engrenagem na barra lateral: a aba de Configurações se posiciona na primeira aba (índice 0).
5. **Teste de desinstalação (se desejado):**
   * Execute o desinstalador em `Configurações > Aplicativos Instalados > KyraFiles` (ou `%LocalAppData%\Programs\KyraFiles\unins000.exe`).
   * Verifique que a pasta é removida e o `Win + E` volta ao Explorer nativo.

## O que ainda não está no exe / Próximos passos

- **F021:** Próxima funcionalidade a ser implementada na esteira do SDD conforme prioridades do backlog.
