# F032 — Plano

Status: **APROVADO** (Claude, 21/10/2026).

## Arquitetura lida (CONFIRMED por leitura)
- `UserControls/TabBar/TabBarItem.cs`: item observável da aba (cabeçalho, ícone, `NavigationParameter`).
- `UserControls/TabBar/TabBar.xaml`: `TabView` com `TabItemTemplate` (`TabViewItem`, `ContextFlyout="{StaticResource TabFlyout}"`); menu da aba com Nova aba, Duplicar, Mover para nova janela, Fechar à esquerda/direita/outras, Reabrir.
- `UserControls/TabBar/BaseTabBar.cs`: `CloseTab(TabBarItem?)` é o ponto único de fechamento (botão, `TabCloseRequested`, ações).
- `Helpers/Navigation/MultitaskingTabsHelpers.cs`: fechar à esquerda/direita/outras/todas e mover para nova janela chamam `CloseTab`.
- Sessão: `Helpers/Application/AppLifecycleHelper.cs` salva `GeneralSettingsService.LastSessionTabList` (lista de `TabBarItemParameter.Serialize()`, JSON com `InitialPageType` e `NavigationParameter`); `MainPageViewModel.RestoreSessionTabsAsync` restaura.

## Abordagem
1. `TabBarItem.IsPinned` (observável).
2. `TabBarItemParameter`: campo `IsPinned` serializado (opcional; JSON antigo sem o campo continua válido).
3. `BaseTabBar.CloseTab`: ignora aba fixada; método explícito para o caso de mover para nova janela (desafixa e fecha).
4. Comandos novos `PinTab`/`UnpinTab` (ações do padrão `GeneratedRichCommand`, sobre a aba do menu) que ajustam `IsPinned` e reposicionam em `MainPageViewModel.AppInstances`.
5. `TabBar.xaml`: itens "Fixar/Desafixar" no menu; `IsClosable="{x:Bind IsPinned, Converter=negação}"` no `TabViewItem` e alfinete no `IconSource`/sobreposição.
6. Reordenação: depois de `TabDragCompleted`/`TabItemsChanged`, reordenar de forma estável (fixadas antes das comuns).
7. Sessão: salvar e restaurar `IsPinned`.

## Arquivos
`TabBarItem.cs`, `TabBarItemParameter.cs`, `BaseTabBar.cs`, `TabBar.xaml(.cs)`, `MultitaskingTabsHelpers.cs`, `AppLifecycleHelper.cs`, `MainPageViewModel.cs`, ações novas em `Actions/Navigation/`, `Strings/en-US/Resources.resw` (+ pt-BR). **Não mexer:** `ShellPanesPage`, navegação dentro da aba.

## Riscos
`TabView` gerado com `IsClosable` ligado ao item: conferir que Ctrl+W (ação `CloseSelectedTab`) também passa por `CloseTab`.

## Testes
AC-1 a AC-5 abrindo o app e usando o menu da aba (UI Automation e captura), só com o computador ocioso.
