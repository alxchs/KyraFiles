# F034 — Plano

Status: **APROVADO** (Claude, 23/10/2026).

## Arquitetura atual (CONFIRMED, lida no código em 23/10/2026)
- `DetailsLayoutPage.xaml` (1 DataTemplate) e `GridLayoutPage.xaml` (3 DataTemplates: Grade, Lista, Cartões) declaram um `CheckBox x:Name="SelectionCheckbox"` com `Opacity="0"` em toda linha; o estado visual `ShowCheckbox` o torna opaco (e em Detalhes/Lista esconde o `IconBox`).
- `UpdateCheckboxVisibility` (nas duas páginas) mostra a caixa com o mouse em cima "regardless of the setting" (comentário do upstream); a opção só controla a caixa das linhas selecionadas.
- Detalhes: `FileList_ContainerContentChanging` acha a caixa por `FindDescendant` e liga 5 eventos (F027); `OnSelectionChanged` atualiza `SelectAllCheckbox` (cabeçalho, `x:Name="SelectAllCheckbox"`) a cada mudança.
- Grid: `FileList_ContainerContentChanging` desliga e religa eventos e chama `SetCheckboxSelectionState` (busca + `GoToState`) por linha; `OnSelectionChanged` faz o mesmo para cada item adicionado/removido.
- `FoldersSettingsService.ShowCheckboxesWhenSelectingItems` = `Get(true)`; trocá-la já dispara `ShellViewModel.RefreshItems` (as linhas são realizadas de novo).
- O upstream já usa `x:Load="False"` dentro desses DataTemplates (`EditPopup`) e o realiza com `FindName` (`GridLayoutPage.xaml.cs`).

## Abordagem
1. `x:Load="False"` nas 4 caixas das linhas e na do cabeçalho: com a opção desligada o elemento nem é criado (FR-1, FR-2).
2. Uma propriedade `ShowSelectionCheckboxes` (lê a opção) e, nas duas páginas, todo o código de caixa só roda se ela for verdadeira (FR-3). Desligada, `ContainerContentChanging` e `OnSelectionChanged` retornam antes de qualquer busca.
3. Ligada: a caixa da linha é realizada por `FindName` no `ContentTemplateRoot` da linha (cache por container, como hoje) e o cabeçalho por `FindName("SelectAllCheckbox")` (FR-5).
4. Desligar com caixas já criadas (containers reciclados): a caixa em cache recebe `Visibility.Collapsed`, sem busca na árvore; o hover deixa de mostrar caixa quando a opção está desligada (FR-6).
5. `Get(true)` → `Get(false)` (FR-4). Valor gravado pelo usuário prevalece (é como o `UserSettingsService` funciona).

**Descartado:** apagar as caixas do XAML e o código (aumenta a divergência do upstream e tira de quem usa toque); só `Visibility` sem `x:Load` (a caixa continuaria sendo criada em toda linha).

## Mapa requisito → código
| Req | Onde |
|---|---|
| FR-1, FR-2 | `DetailsLayoutPage.xaml`, `GridLayoutPage.xaml` (`x:Load`) |
| FR-3, FR-5, FR-6 | `DetailsLayoutPage.xaml.cs`, `GridLayoutPage.xaml.cs` |
| FR-4 | `FoldersSettingsService.cs` |

**Não mexer:** `BaseLayoutPage.cs` (F028/F030), bandeiras da F029 em `DetailsLayoutPage.xaml.cs`, a tela de Configurações (a opção já existe).

## Riscos
- Setter de `VisualState` que mira elemento com `x:Load` o realiza: o `GoToState("ShowCheckbox")` só pode ser chamado com a opção ligada.
- Código que lia o campo `SelectAllCheckbox` direto (margem no modo compacto): passa a checar nulo e aplicar a margem ao realizar.

## Teste por AC
AC-1 a AC-5 no app aberto (UI Automation: procurar `CheckBox` dentro das linhas e no cabeçalho, mais captura); AC-6 com `user_settings.json` sem a chave. Build Release 0/0.
