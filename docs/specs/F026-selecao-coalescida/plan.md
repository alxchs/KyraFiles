# F026 — Plano

Status: **APROVADO** (Claude, 20/10/2026).

## Arquitetura lida no código (CONFIRMED por leitura em 20/10/2026)
- `BaseLayoutPage.cs` (setter de `SelectedItems`, ~l.272-325): a cada mudança chama `UpdateSelectionSize()`, atualiza `SelectedItemsPropertiesViewModel` (contagem, string), agenda `Task.Delay(50)`, e **sempre** `ParentShellPageInstance.ToolbarViewModel.SelectedItems = value`.
- `NavigationToolbarViewModel.cs` (~l.267-282): o setter de `SelectedItems` dispara `OnPropertyChanged` de `CanExtract` e `HasAdditionalAction` e faz `IsDynamicOverflowEnabled = false; = true` (workaround que força re-medição do toolbar). **INFERRED:** esse é o maior custo por passo; precisa de A/B.
- `BaseGroupableLayoutPage.cs:252` `FileList_SelectionChanged` chama o setter.

## Abordagem
Atrasar só a parte cara com um `DispatcherQueueTimer` de 80 ms (reiniciado a cada mudança): no setter, se a mudança vier em sequência rápida, agenda `ToolbarViewModel.SelectedItems = selectedItems` e `UpdateSelectionSize()`+contagem para o disparo do timer. A seleção, o foco e a rolagem não mudam. Antes de qualquer ação por comando, `FlushSelectionToToolbar()` aplica o pendente (FR-4); o método já existe.
Opção descartada: descartar eventos de tecla (perde passos; muda comportamento).

## Arquivos a mexer
`src/Files.App/Views/Layouts/BaseLayoutPage.cs` (setter e timer). Nenhum outro.
## Não mexer
`NavigationToolbarViewModel.cs`, XAML, serviços, `DetailsLayoutPage.xaml.cs` (handlers de tecla da F024 ficam como estão neste experimento).

## Riscos
Toolbar atrasado em até 80 ms; comando executado antes do flush (mitigado pelo flush em `FlushSelectionToToolbar` chamado antes de comandos de teclado e pelo timer curto).

## Testes
AC-1: `f025_hold_key.ps1` + `f025_thr_hold.ps1`, A-B-A, 5 rodadas. AC-2 a AC-4: manual com captura/UI Automation.
