# F047 — Limpar pastas pesadas de build — T0: Confirmação do Plan no Código

**Data:** 2026-10-25  
**Autor:** AGY  
**Branch:** `feature/t0-confirmacoes`  
**Status:** CONFIRMADO (SOMENTE LEITURA — T0 Concluída)

---

## 1. Verificação das Afirmações do Plan

### 1.1 Padrão de Diálogo no Projeto
- **Status:** **CONFIRMED**
- **Comando:**
  ```powershell
  Get-ChildItem src/Files.App/Dialogs/*BulkRename*
  git grep -n "BulkRenameDialog" src/Files.App/
  ```
- **Saída:**
  ```text
  src/Files.App/Dialogs/BulkRenameDialog.xaml
  src/Files.App/Dialogs/BulkRenameDialog.xaml.cs
  src/Files.App/Services/App/AppDialogService.cs:30: { typeof(BulkRenameDialogViewModel), () => new BulkRenameDialog() },
  src/Files.App/ViewModels/Dialogs/BulkRenameDialogViewModel.cs:8
  ```
- **Evidência no código:**
  O padrão oficial do projeto utiliza um `ContentDialog` em `src/Files.App/Dialogs/`, implementando `IDialog<TViewModel>`, com ViewModel correspondente em `src/Files.App/ViewModels/Dialogs/`, registrado no dicionário de tipos de `AppDialogService.cs`.

### 1.2 Rotina de Exclusão para a Lixeira e Exclusão Definitiva
- **Status:** **CONFIRMED**
- **Comando:**
  ```powershell
  git grep -n "DeleteItemsAsync" src/Files.App/Actions/FileSystem/
  ```
- **Saída:**
  ```text
  src/Files.App/Actions/FileSystem/BaseDeleteAction.cs:42: await shellPage.FilesystemHelpers.DeleteItemsAsync(items, settings.DeleteConfirmationPolicy, permanently, true);
  src/Files.App/Actions/FileSystem/DeleteItemAction.cs:35: return DeleteItemsAsync(false);
  src/Files.App/Actions/FileSystem/DeleteItemPermanentlyAction.cs:26: return DeleteItemsAsync(true);
  ```
- **Evidência no código (`BaseDeleteAction.cs:30-46`):**
  A exclusão para a Lixeira e a exclusão definitiva passam por `FilesystemHelpers.DeleteItemsAsync(items, deleteConfirmationPolicy, permanently, true)`.
  - `permanently = false`: Move para a Lixeira (comportamento padrão e seguro de F047).
  - `permanently = true`: Exclusão direta e definitiva (opção explícita suportada pela infraestrutura existente).

### 1.3 Localização para Serviços Novos
- **Status:** **CONFIRMED**
- **Local:** `src/Files.App/Services/` ou `src/Files.App/Utils/` (ex.: `src/Files.App/Services/BuildFolderCleaner/` contendo `BuildFolderRules` e `BuildFolderScanner`, com DI registrada em `AppLifecycleHelper.cs`).

### 1.4 Registro no Menu de Contexto (Item Selecionado e Fundo)
- **Status:** **CONFIRMED**
- **Comando:**
  ```powershell
  git grep -n "GetItemContextCommandsWithoutShellItems" src/Files.App/Views/Layouts/BaseLayoutPage.cs
  ```
- **Evidência no código (`BaseLayoutPage.cs:753` e `1017`, `ContentPageContextFlyoutFactory.cs:115-140`):**
  - Quando itens estão selecionados, `itemViewModel` é nulo e `itemsSelected = true`.
  - Quando o clique é no fundo (área vazia), `itemViewModel` é fornecido (`shellViewModel`) e `itemsSelected = false`.
  - Portanto, uma ação visível em pasta selecionada e no fundo da pasta utiliza a condição `(!itemsSelected || areAllItemsFolders) && !currentInstanceViewModel.IsPageTypeRecycleBin`.

---

## 2. Conclusão da T0 de F047
Todas as premissas arquiteturais do plan foram confirmadas no código.
Nenhum arquivo ou código de produção foi alterado nesta etapa.
Pronto para T1 quando autorizado.
