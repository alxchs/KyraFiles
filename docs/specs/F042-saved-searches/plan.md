# F042 — Buscas salvas na barra lateral — PLAN

Status: APROVADO no Lote 1 (`D-TRAIN-1`, 18/11/2026). Spec aprovada pelo Alexandre em 24/10/2026.

## 1. Arquitetura atual (lida no código em 18/11/2026 — CONFIRMED)

- **Barra lateral:** `SidebarView` (`src/KyraFiles.App.Controls/Sidebar/SidebarView.xaml`), `SidebarViewModel` (`src/KyraFiles.App/ViewModels/UserControls/SidebarViewModel.cs`), `INavigationControlItem` / `SectionType` / `NavigationControlItemType` (`src/KyraFiles.App/Data/Contracts/INavigationControlItem.cs`).
  - Itens virtuais sem pasta física seguem o padrão de `FileTagItem` (`src/KyraFiles.App/Data/Items/FileTagItem.cs`) + `FileTagsManager` (`src/KyraFiles.App/Utils/FileTags/FileTagsManager.cs`, evento `DataChanged`).
  - `SidebarItem.cs:145` e `264-290` atualmente habilitam `CanDrag` apenas quando `Path.IsPathRooted(path)`; para reordenação por arraste (`FR-003`), caminhos virtuais `savedsearch:<id>` precisam de suporte explícito em `SidebarItem` / `SidebarViewModel`.
- **Pipeline de busca e filtros relativos:**
  - `src/KyraFiles.Shared/Helpers/SearchIntent.cs`, `NaturalSearchParser.cs` (`Parse(query, DateTimeOffset referenceDate)`) e `NaturalSearchPostFilter.cs` já resolvem datas relativas (`hoje`, `últimos N dias`, etc.), extensões e tamanhos com data de referência dinâmica.
  - `ISearchProviderFactory.GetCurrentProvider()` (`src/KyraFiles.App/Services/Search/SearchProviderFactory.cs`) roteia entre `EverythingSearchProvider` e `NativeFilesSearchProvider`. Atenção: `NativeFilesSearchProvider.SearchAsync` (L45) retorna vazio se `request.Query` estiver em branco — para buscas somente por filtro, usar curinga `"*"` como `effectiveQuery`.
  - Cabeçalho de resultados: `Grid x:Name="SearchHeader"` em `src/KyraFiles.App/Views/Shells/ModernShellPage.xaml:106-210` (visível quando `ShellViewModel.IsSearchResults`), ponto natural para o botão **"Salvar busca"**.
- **Persistência e DI:**
  - `ApplicationData.Current.LocalFolder.Path` + subpasta `Constants.LocalSettings.SettingsFolderName` (`"settings"`), serialização AOT-safe via `JsonSerializerContext` (`[JsonSerializable]`).
  - Registros DI em `src/KyraFiles.App/Helpers/Application/AppLifecycleHelper.cs:308-426`.

## 2. Abordagem

1. **Núcleo puro em `KyraFiles.Shared/SavedSearches/`:**
   - `SavedSearchDefinition`: `Id`, `Name`, `Query`, `ScopePath`, `IncludeSubfolders`, `Extensions`, `MinSizeBytes`, `MaxSizeBytes`, `RelativeDateKind` (`None`, `Today`, `Yesterday`, `LastNDays`, `ThisWeek`, `ThisMonth`), `RelativeDaysCount`, `AbsoluteDateFrom`, `AbsoluteDateTo`, `Order`.
   - `SavedSearchQueryEvaluator`: dado um `SavedSearchDefinition` e um `DateTimeOffset now` (injetável para teste de AC-2!), resolve o `SearchIntent` efetivo (`DateModifiedRange` sempre recalculado a partir de `now`, `SizeRange`, `FileExtensions`, `SearchText` com fallback `"*"` quando há filtros sem texto) e valida se `ScopePath` existe no disco (AC-3).
   - `SavedSearchStore`: lista ordenada em `saved-searches.json` (AOT-safe `SavedSearchJsonContext`), escrita atômica, CRUD (`Add`, `Update`, `Remove`, `Reorder(fromIndex, toIndex)`), proteção contra nomes duplicados e leitura tolerante a JSON corrompido (AC-4, AC-5, AC-7, FR-003).
2. **Integração no `KyraFiles.App`:**
   - `ISavedSearchesService` / `SavedSearchesService` (singleton em `AppLifecycleHelper.cs`) + `SavedSearchSidebarItem : ObservableObject, INavigationControlItem`.
   - Nova seção `SectionType.SavedSearches` no `SidebarViewModel` (menu de contexto: Abrir, Editar, Renomear, Mover para cima/baixo, Remover sem apagar arquivos; arrastar para reordenar).
   - Botão **"Salvar busca"** no `SearchHeader` da `ModernShellPage.xaml` capturando texto, filtros ativos e escopo atual.
   - Ao abrir busca salva: valida `ScopePath`; se ausente, exibe aviso claro com opção direta de editar o escopo (AC-3); caso contrário executa `ShellViewModel.SearchAsync` via `ISearchProviderFactory.GetCurrentProvider()` com `SearchIntent` recém-avaliado (AC-2, AC-6, FR-002).

## 3. Arquivos a mexer e a não mexer
- **Mexer:** novos em `src/KyraFiles.Shared/SavedSearches/`; `SavedSearchesService`, `SavedSearchSidebarItem`, `INavigationControlItem.cs`, `SidebarViewModel.cs`, `SidebarItem.cs`, `ModernShellPage.xaml(.cs)`, `AppLifecycleHelper.cs`, `Strings/en-US` & `Strings/pt-BR`, testes em `tests/KyraFiles.App.Search.Index.Tests/SavedSearchesTests.cs`.
- **Não mexer:** `NaturalSearchParser` (reutilizar como está), `EverythingSearchProvider` / `NativeFilesSearchProvider` (apenas passar `"*"` quando consulta só tiver filtros), `Directory.Build.props`.

## 4. Estratégia de teste (por AC)
- **Unitários (`SavedSearchesTests.cs`):** AC-1 (salvar com filtros), AC-2 (reavaliar `LastNDays=7` em `dia D` vs `dia D+1` produz janelas de data distintas), AC-3 (escopo inexistente retorna `ScopeMissing = true` sem exceção), AC-4/AC-5/AC-7 (editar, remover sem tocar arquivos, reordenar e persistir/recarregar JSON), caso de borda nome repetido e JSON corrompido.
- **Lote/UI (`train-v4.3.22`):** salvar busca no `SearchHeader`, clicar na barra lateral, editar e remover na validação consolidada do Train.
