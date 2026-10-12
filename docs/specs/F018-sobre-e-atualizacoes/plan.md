# F018 — PLAN

## Arquitetura lida no código (12/10/2026)
- CONFIRMED: menu Ajuda em `src/Files.App/UserControls/Menus/ClassicMenuBar.xaml` (itens ligados a `Commands.*`).
- CONFIRMED: ações são classes com `[GeneratedRichCommand]` em `src/Files.App/Actions/` (ex.: `OpenReleaseNotesAction`); o gerador cria a entrada em `ICommandManager`.
- CONFIRMED: Configurações abrem numa página específica por `OpenSettingsAction` com `SettingsNavigationParams { PageKind }`; `SettingsPageKind.AboutPage` existe.
- CONFIRMED: `IUpdateService` é `DummyUpdateService` fora dos ambientes Store/Sideload (`AppLifecycleHelper.cs` ~407), sem verificação real. Não será alterado.
- CONFIRMED: diálogo simples: `DialogDisplayHelper.ShowDialogAsync(título, mensagem, primário, secundário)`.

## Abordagem
Verificador próprio e pequeno, sem mexer no `IUpdateService` do upstream: `Services/App/ReleaseCheckService` consulta `https://api.github.com/repos/alxchs/FilesCustom/releases/latest`, extrai a versão do `tag_name` (primeiro padrão `n.n[.n[.n]]`) e compara com `Package.Current.Id.Version`. Resultados: UpToDate, Available, NoReleases (404), Failed.
Opções descartadas: trocar o `IUpdateService` (acopla ao fluxo Store/Sideload do upstream); verificação automática (fora do escopo, AC-7).

## Mapa
| Requisito | Código |
|---|---|
| FR-001 | `Actions/Open/OpenAboutAction.cs` + item em `ClassicMenuBar.xaml` |
| FR-002 | `Actions/Open/CheckForUpdatesAction.cs` + item em `ClassicMenuBar.xaml` |
| FR-003 | botão em `Views/Settings/AboutPage.xaml` + comando em `AboutViewModel.cs` |
| FR-004 | `Strings/en-US` e `Strings/pt-BR` |
| AC-3..6 | `ReleaseCheckService` + `DialogDisplayHelper` |

Não mexer: `Services/App/AppUpdate*.cs`, `AppLifecycleHelper` (DI), F015/F017.

## Riscos
Repositório de destino sem releases (hoje `latestRelease: null`) -> AC-5. A URL é constante em `Constants.ExternalUrl`.

## Teste por AC
AC-1/2/3/5: abrir o app e usar (captura de tela, UI Automation). AC-4: simular releaseNotFound/maior versão apontando a constante para um servidor local de teste. AC-6: URL inválida. AC-7: revisão do código (nenhum chamador automático).
