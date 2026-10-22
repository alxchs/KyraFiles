# F030 — Plano

Status: **APROVADO** (Claude, 21/10/2026).

## Arquitetura lida (CONFIRMED por leitura)
- `ViewModels/UserControls/InfoPaneViewModel.cs`: `ContentPageContext_PropertyChanged` reage a `SelectedItem`, espera 100 ms e chama `UpdateSelectedItemPreviewAsync`; `UnloadPreview()` cancela a carga e limpa `PreviewPaneContent`.
- `Views/Layouts/DetailsLayoutPage.xaml.cs`: a F028 já detecta a repetição (2º `KeyDown` sem `KeyUp`, `heldNavigationKey`) e o estado real da tecla (`IsNavigationKeyHeld`); `BaseLayoutPage` encerra o adiamento 200 ms depois de soltar (`scrollSettleTimer`).
- `Data/Models/AppModel.cs`: modelo global observável (`App.AppModel`).

## Abordagem
1. `AppModel.IsNavigationKeyHeld` (observável).
2. `DetailsLayoutPage`: liga no 1º passo repetido; `BaseLayoutPage`, no fim do adiamento (tecla solta), desliga.
3. `InfoPaneViewModel`: ao ligar, `UnloadPreview()` e estado "sem preview"; enquanto ligado, ignora mudanças de seleção; ao desligar, refaz a leitura da seleção e chama `UpdateSelectedItemPreviewAsync` uma vez.

## Arquivos
`AppModel.cs`, `DetailsLayoutPage.xaml.cs`, `BaseLayoutPage.cs`, `InfoPaneViewModel.cs`. **Não mexer:** XAML do painel, `MainPage` (largura do painel).

## Riscos
Tecla solta com o foco fora da lista: o desligamento usa o mesmo fim de adiamento que lê o estado real da tecla (`IsNavigationKeyHeld` da F028), então não fica preso.

## Testes
AC-1 e AC-2 por captura de quadros com o painel aberto; AC-3 com `f025_hold_key.ps1`, painel aberto e fechado no mesmo lote.
