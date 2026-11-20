# F033 — Plano

Status: **APROVADO** (Claude, 21/10/2026).

## Arquitetura lida (CONFIRMED por leitura)
- `Data/Factories/ContentPageContextFlyoutFactory.cs`: monta o menu de contexto com `ContextMenuFlyoutItemViewModel` (com `Items` para submenu, `Command`/`CommandParameter`) e `ContextMenuFlyoutItemViewModelBuilder(Commands.X)`; "Abrir em novo painel" já usa submenu com parâmetro.
- `Utils/Storage/Operations/FilesystemHelpers.cs`: `CopyItemsAsync`/`MoveItemsAsync(IEnumerable<IStorageItemWithPath>, IEnumerable<string> destinos, showDialog, registerHistory)`; `UIFilesystemHelpers.CreateFolderWithSelectionAsync` mostra o uso (`StorageHelpers.FromPathAndType` + `PathNormalization.Combine`).
- `Data/Contracts/ICommonDialogService.cs`: `Open_FileOpenDialog(hWnd, pickFoldersOnly: true, ...)` abre o seletor de pasta.
- Configurações: `IGeneralSettingsService`/`GeneralSettingsService` guardam listas (`LastSessionTabList` é `List<string>?`).

## Abordagem
1. `GeneralSettingsService.RecentCopyMoveDestinations` (`List<string>?`), com serviço pequeno `CopyMoveDestinationHistory` (adicionar no topo, sem repetição, máximo 12, filtrar inexistentes).
2. Ação `CopyToFolderAction` e `MoveToFolderAction` (`IAction`, `GeneratedRichCommand`), parâmetro = caminho de destino ou nulo (nulo abre o seletor). Fluxo: destino → confirmação (`ContentDialog` com `DefaultButton = Close` = "Não") → `CopyItemsAsync`/`MoveItemsAsync` com `registerHistory: true` → registrar destino.
3. `ContentPageContextFlyoutFactory`: dois itens com submenu, gerados na abertura do menu a partir da lista (só itens selecionados; fora da Lixeira).
4. Textos em `Strings/en-US/Resources.resw` e `pt-BR`.

## Arquivos
`IGeneralSettingsService.cs`, `GeneralSettingsService.cs`, serviço novo em `Services/`, ações novas em `Actions/FileSystem/Transfer/`, `ContentPageContextFlyoutFactory.cs`, recursos de texto. **Não mexer:** `FilesystemHelpers` (só usar).

## Riscos
- Confirmação com `ContentDialog`: só um diálogo pode estar aberto por vez; usar o serviço de diálogos do app.
- Pastas de rede lentas no filtro de existência: verificar com tempo limite curto e, se estourar, mostrar a pasta mesmo assim.

## Testes
AC-1 a AC-6 no app aberto com pastas do laboratório (`C:\KyraFilesUXLab`), só com o computador ocioso; Ctrl+Z conferido.
