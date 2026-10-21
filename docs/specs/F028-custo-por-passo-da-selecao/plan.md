# F028 — Plano

Status: **APROVADO** (Claude, 20/10/2026).

## Arquitetura lida (CONFIRMED por leitura)
- `Services/Settings/DevToolsSettingsService.cs`: `IDEPath`/`IDEName` usam `Get(SoftwareHelpers.IsVSCodeInstalled() ? ... : ...)`; o argumento padrão é avaliado em toda leitura.
- `Helpers/Environment/SoftwareHelpers.cs`: `IsVSCodeInstalled()` enumera `Uninstall` em HKCU e HKLM abrindo cada subchave.
- `Actions/Content/PreviewPopup/LaunchPreviewPopupAction.cs`: a cada `SelectedItems` muda chama `SwitchPopupPreviewAsync` → `GetProviderAsync()` → `DetectAvailability` de QuickLook (pipe), Seer e PowerToys (registro).
- `Views/Layouts/DetailsLayoutPage.xaml.cs` `FileList_PreviewKeyDown`: ramos `PageDown`/`PageUp` (F024) chamam `SetSelectedItem` + `ScrollIntoView` + `FocusSelectedItems`.

## Abordagem
1. Cache do resultado de `IsVSCodeInstalled()` (campo estático preguiçoso).
2. `SwitchPopupPreviewAsync` com debounce: contador de versão + `Task.Delay(150)`; só a última mudança executa.
3. Remover os ramos `PageDown`/`PageUp` do handler (nativo assume).
Descartado: cache com TTL do provedor (mantém detecção por mudança de seleção).

## Arquivos
`SoftwareHelpers.cs`, `LaunchPreviewPopupAction.cs`, `DetailsLayoutPage.xaml.cs`. **Não mexer:** `Toolbar.xaml.cs`, `BaseLayoutPage.cs`.

## Riscos
Instalar o VS Code com o app aberto só aparece após reiniciar; preview acompanha com 150 ms de atraso.

## Testes
AC-1 pelos scripts F025 e trace Speedscope; AC-2 a AC-4 manual/UI Automation.

## Histórico (20/10/2026, ampliação)
- **FR-5:** `DetailsLayoutPage.FileList_PreviewKeyDown`: em repetição (`KeyStatus.WasKeyDown`) de Up/Down/PgUp/PgDn, descarta enquanto `navigationStepPending`; o flag cai no 2º `CompositionTarget.Rendering` depois do passo.
- **FR-6:** `DetailsLayoutPage.xaml`: `CacheLength` removido (padrão 4, igual ao upstream). O 1.0 da F026 deixava o mouse esvaziar a tela.
- `BaseLayoutPage`: `SelectionEffectsBurstMs` 350 e `SelectionEffectsSettleMs` 350. Com 120 ms, o intervalo entre passos (agora esperando o quadro) passava do tempo de espera e o toolbar voltava a ser reconstruído a cada passo (15 linhas em 4 s).
- `LaunchPreviewPopupAction`: no caminho de troca de seleção, o provedor detectado fica guardado 5 s (o trace mostrou a detecção ainda rodando, 302 ms).
