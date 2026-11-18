# F038 — Espaços de trabalho com nome — PLAN

Status: APROVADO no Lote 2 (`D-TRAIN-1`, 18/11/2026). Spec aprovada pelo Alexandre em 24/10/2026 (posição/tamanho de janela fora desta v1 conforme §8).

## 1. Arquitetura atual (lida no código em 18/11/2026 — CONFIRMED)

- **Abas e fixadas:** `MainPageViewModel.AppInstances` (`ObservableCollection<TabBarItem>`), `TabBarItem.IsPinned` / `NavigationParameter` (`TabBarItemParameter`), `MultitaskingTabsHelpers.SetTabPinned` / `KeepPinnedTabsFirst`.
- **Painel duplo:** `PaneNavigationArguments` (`LeftPaneNavPathParam`, `RightPaneNavPathParam`, `ShellPaneArrangement`) já registrado em `AppJsonSerializerContext`; `ShellPanesPage.IsMultiPaneActive`, `NavParamsLeft`, `NavParamsRight`.
- **Layout por pasta:** gerenciado por `LayoutPreferencesManager` (`BaseShellPage.FolderSettings`), não persistido em `TabBarItemParameter`.
- **Armadilhas críticas confirmadas no código:**
  1. `BaseTabBar.CloseTab` fecha `MainWindow.Instance` se `Items.Count == 0` (`BaseTabBar.cs:171-172`) e ignora abas com `IsPinned == true`. Ao substituir abas de um espaço, abrir primeiro as novas abas (ou manter 1 temporária) e só depois despinar/fechar as antigas!
  2. `NavigationHelpers.AddNewTabByParamAsync` chama `FindExistingTabIndex` (`NavigationHelpers.cs:239-245`), que pula abas cujo caminho esquerdo já esteja aberto. No modo "Substituir abas atuais", fechar antigas (com segurança acima) ou usar sobrecarga de restauração de espaço para não perder abas.

## 2. Abordagem

1. **Núcleo puro AOT-safe em `src/KyraFiles.Shared/Workspaces/`:**
   - `WorkspaceTabSnapshot` (`LeftPath`, `RightPath`, `IsDualPane`, `PaneArrangement`, `IsPinned`, `LayoutModeName`, `SortOptionName`).
   - `NamedWorkspace` (`Id`, `Name`, `Tabs`, `SelectedTabIndex`, `CreatedAtUtc`, `UpdatedAtUtc`).
   - `NamedWorkspaceStore` (`workspaces.json` em `LocalFolder\settings\`, `JsonSerializerContext`, escrita atômica, CRUD, detecção de nome duplicado AC-5, validação de pastas ausentes sem derrubar abertura AC-3).
2. **Integração no `KyraFiles.App`:**
   - `INamedWorkspacesService` / `NamedWorkspacesService` (captura snapshot de `MainPageViewModel.AppInstances`, aplica modo "Substituir abas atuais" ou "Adicionar às abas atuais" — AC-6, preservando abas fixadas e painel duplo — AC-2, avisando pastas ausentes — AC-3).
   - `WorkspacesDialog` + `OpenWorkspacesAction` (`[GeneratedRichCommand]`, acessível na paleta de comandos e menu).

## 3. Estratégia de teste
- **Unitários (`NamedWorkspacesTests.cs`):** AC-1 a AC-6 (salvar 4 abas + 2 fixadas + painel duplo, atualizar espaço, conflito de nome duplicado, pasta ausente sinalizada sem exceção, round-trip JSON, controle negativo espaço vazio).
- **Lote/UI (`train-v4.3.23`):** salvar e reabrir espaço de trabalho na validação consolidada.
