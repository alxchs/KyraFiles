# F027 — Plano

Status: **APROVADO** (Claude, 20/10/2026).

## Arquitetura lida (CONFIRMED por leitura em 20/10/2026) — `DetailsLayoutPage.xaml.cs`
- `OnSelectionChanged` (~l.445): para cada item adicionado/removido chama `SetCheckboxSelectionState` e depois `UpdateSelectAllCheckboxState`.
- `FileList_ContainerContentChanging` (~l.1380): a cada criação/reciclagem de contêiner, desinscreve 5 e inscreve 3 eventos do checkbox, acha o checkbox (`GetSelectionCheckbox`) e chama `SetCheckboxSelectionState`.
- `SetCheckboxSelectionState` (~l.1423): `FindDescendant` do checkbox, desinscreve/inscreve `Checked`/`Unchecked`, `FileList.SelectedItems.Contains(item)` (O(n) no WinRT), `FindDescendant<UserControl>` e `VisualStateManager.GoToState(..., true)` (com transição).
- `SelectionCheckbox_PointerEntered` (~l.1499): mostra o checkbox.

## Abordagem
1. `OnSelectionChanged` deixa de pintar item a item; mantém `UpdateSelectAllCheckboxState`.
2. `FileList_ContainerContentChanging` deixa de mexer em eventos e estado do checkbox a cada realização; inscreve os eventos **uma vez por checkbox** (tabela de "já ligado") e só.
3. Pintura **preguiçosa**: em `SelectionCheckbox_PointerEntered` chama `SetCheckboxSelectionState(item, container)` antes de mostrar.
Opção descartada: remover o checkbox (decisão do Alexandre se o teste indicar).

## Arquivos a mexer
`src/Files.App/Views/Layouts/DetailsLayoutPage.xaml.cs` somente. **Não mexer:** `GridLayoutPage`, XAML, `BaseLayoutPage`.

## Riscos
Checkbox obsoleto visível em contêiner reciclado sob o ponteiro até o próximo entra/sai do ponteiro; linha selecionada sem checkbox visível até passar o ponteiro.

## Testes
AC-1: scripts F025 (5 rodadas, mesma pasta). AC-2 a AC-4: UI Automation e captura de tela.
